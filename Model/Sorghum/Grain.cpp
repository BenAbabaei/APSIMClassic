//------------------------------------------------------------------------------------------------
#include <stdio.h>
#include "Grain.h"
#include "Plant.h"

using namespace Sorghum;
//---------------------------------------------------------------------------
//------ Grain Constructor
//------------------------------------------------------------------------------------------------
Grain::Grain(ScienceAPI2 &api, Plant *p) : PlantPart(api)
   {
   plant = p;
   name = "Grain";
   partNo = 4;

   doRegistrations();
   initialize();
   }
//------------------------------------------------------------------------------------------------
//------ Grain Destructor
//------------------------------------------------------------------------------------------------
Grain::~Grain()
   {

   }
//--------------------------------------------------------------------------------------------------
// Register variables for other modules
//--------------------------------------------------------------------------------------------------
void Grain::doRegistrations(void)
   {
   scienceAPI.expose("GrainGreenWt",    "g/m^2",      "Live grain dry weight",    false, dmGreen);
   scienceAPI.expose("GrainNo",         "grains/m^2", "Grain number",             false, grainNo);
   scienceAPI.expose("GrainSize",       "g/1000grain","1000 grain weight",        false, grainSize);
   scienceAPI.expose("GrainGreenN",     "g/m^2",      "N in grain",               false, nGreen);
   scienceAPI.expose("GrainGreenNConc", "%",          "N concentration in grain", false, nConc);
   scienceAPI.expose("Yield",           "kg/ha",      "Grain yield",              false, yield);
   scienceAPI.expose("GrainGreenP",     "g/m^2",      "P in live Grain",          false, pGreen);
   scienceAPI.expose("GrainTempFactor", "()",         "Stress on Grain Number",   false, tempFactor);
   scienceAPI.expose("DltDMGrainDemand","g/m^2",      "Delta DM Grain Demand",    false, dltDMGrainDemand);
   scienceAPI.expose("PotGrainFillRate","mg/grain/oCd","Potential Grain Fill Rate",false, potGFRate);

   }
//------------------------------------------------------------------------------------------------
//------- Initialize variables
//------------------------------------------------------------------------------------------------
void Grain::initialize(void)
   {
     /* TODO : Fix prepare so that it is not called every day that therree is no crop in */
   grainNo = 0.0;
   finalGrainNo = 0.0;
   grainSize = 0.0;
   dltDMGrainDemand = 0.0;
   yield = 0.0;
   tempFactor = 1.0;
	totDMGreenFI = 0.0;
   potGFRate = 0.0;
   dltDMStressMax = 0.0;

   PlantPart::initialize();
   }
//------------------------------------------------------------------------------------------------
//------ read Grain parameters
//------------------------------------------------------------------------------------------------
void Grain::readParams (void)
   {
   scienceAPI.read("dm_per_seed", "", 0, dmPerSeed);
   scienceAPI.read("maxGFRate", "", 0, maxGFRate);

   scienceAPI.read("grn_water_cont", "", 0, waterContent);
   // nitrogen
   scienceAPI.read("grainFillRate","", 0, grainFillRate);
   scienceAPI.read("targetGrainNConc", "", 0, targetNConc);

   // phosphorus
   pMaxTable.read(scienceAPI, "x_p_stage_code","y_p_conc_max_grain");
   pMinTable.read(scienceAPI, "x_p_stage_code","y_p_conc_min_grain");
   pSenTable.read(scienceAPI, "x_p_stage_code","y_p_conc_sen_grain");
   scienceAPI.read("p_conc_init_grain", "", 0, initialPConc);   /* TODO : Remove this */

   // heat effects on grain number
   scienceAPI.read("GrainTempWindow","", 0, grainTempWindow);
   scienceAPI.read("GrainTempOrdinals","", 0, grainTempOrdinals);
   vector<double> y;y.push_back(0.0);y.push_back(1.0);
   grainTempTable.load(grainTempOrdinals,y);


   }

//------------------------------------------------------------------------------------------------
//------ update variables
//------------------------------------------------------------------------------------------------
void Grain::updateVars(void)
   {
   // initialise P - must be better way
//   if(dmGreen < 1e-5 && dltDmGreen > 0)
//      pGreen = initialPConc * dltDmGreen;


   dmGreen += dltDmGreen;
   dmGreen += dmRetranslocate;
   nGreen += dltNGreen  + dltNRetranslocate;
   nConc = divide(nGreen,dmGreen,0) * 100.0;

   grainSize = divide (dmGreen, grainNo, 0.0) * 1000.0;
   stage = plant->phenology->currentStage();

   // Ramp grain number from 0 at StartGrainFill to finalGrainNo at SGF + 100dd
   double gfTTNow = plant->phenology->sumTTtotalFM(startGrainFill,maturity);
   grainNo = Min((gfTTNow/100.0 *  finalGrainNo),finalGrainNo) * tempFactor;

   yield = dmGreen * 10.0;                   // yield in kg/ha for reporting


   }
//------------------------------------------------------------------------------------------------
//------- react to a phenology event
//------------------------------------------------------------------------------------------------
void Grain::phenologyEvent(int iStage)
   {
   switch (iStage)
      {
      case emergence :
         break;
      case fi :
         totDMGreenFI = plant->biomass->getTotalBiomass();                  // for grain number
         break;
      case startGrainFill :
         finalGrainNo = calcGrainNumber();
         break;
      }
   }
//------------------------------------------------------------------------------------------------
void Grain::process(void)
   {
   // calculate high temperature effects on grain number
   if(stage >= fi && stage <= flowering)
      {
      tempFactor -= calcTempFactor();
      tempFactor = bound(tempFactor,0.0,1.0);
      }

   // calculate grain biomass demand
   if(stage >= startGrainFill && stage <= endGrainFill)
      {
      calcDemandStress();
      calcBiomassDemand();
      }
   }
//------------------------------------------------------------------------------------------------
double Grain::calcTempFactor(void)
   {
   // calculate a daily contribution to stress on grain number
   // if we are within the grain stress window (grainTempWindow)calculate stress factor
   // from grainTempTable and this day's contribution to the total stress

   // first see if it is a hot day
   if(grainTempTable.value(plant->today.maxT) < 0.001)return 0.0;

   // then see if we are in the pre flag or post-flag window window
   // if not return 0                                      (grainTempWindow[0] is -ve)
   double targetTT = plant->phenology->sumTTtarget (fi, flag) + grainTempWindow[0];
   double eTT = plant->phenology->sumTTtotal (fi, flag);
   if(eTT < targetTT)return 0.0;
   // see if in the post flag window
   double eTTpostFlag = plant->phenology->sumTTtotal (flag, flowering);
   if(eTTpostFlag > grainTempWindow[1]) return 0.0;

   double dltTT = plant->phenology->getDltTT();
   double ttContrib;
   // check  window
   if(eTTpostFlag > 0.0)  // post flag
      ttContrib = Min(grainTempWindow[1] - eTTpostFlag, dltTT);      // allow for overlap
   else                   // pre flag
      ttContrib = Min(eTT - targetTT, dltTT);      // allow for overlap

   double dayFract = ttContrib / (-grainTempWindow[0] + grainTempWindow[1]);
   return dayFract * grainTempTable.value(plant->today.maxT);
   }
//------------------------------------------------------------------------------------------------
void Grain::calcDemandStress(void)
   {
   // for HI approach ?
   /* TODO : See if this needs updating here Should not happen*/
//   plant->water->photosynthesisStress = divide(plant->water->totalSupply,
//                           plant->water->swDemand,1.0);

   dltDMStressMax = yieldPartDemandStress();
   }
//------------------------------------------------------------------------------------------------
void Grain::calcBiomassDemand(void)
   {
   // source sink (grain number approach)
   dltDMGrainDemand = calcDMGrainSourceSink();
   }
//------------------------------------------------------------------------------------------------
//------- calc N demand
//------------------------------------------------------------------------------------------------
//     GRAIN demand to keep grain N filling rate at 0.001mg/grain/dd up to halfway
//       between sgf and maturity where dd is degree days from start_grain_fill
//       then target [N] (1.75%)
double Grain::calcNDemand(void)
   {
   nDemand = 0.0;
   // if not in grain fill, no demand
   if(stage < startGrainFill)return nDemand;


   // for the first half of grainfilling, the demand is calculated on a grain
   // filling rate per grain per oCd
   // rest on target N concentration

   double gfFract = divide(plant->phenology->sumTTtotal(startGrainFill, maturity),
                        plant->phenology->sumTTtarget(startGrainFill, maturity));

   if(gfFract < 0.5)
      nDemand = grainNo * plant->phenology->getDltTTFM() * grainFillRate / 1000.0;
   else
      nDemand = dltDmGreen * targetNConc;

   nDemand = Max(nDemand,0.0);
   return nDemand;
   }
//------------------------------------------------------------------------------------------------
double Grain::calcGrainNumber(void)
   {
   // increase in plant biomass between fi and start grain fill
   double dltDMPlant = plant->biomass->getTotalBiomass()  - totDMGreenFI;

   // growth rate per day
   double nDays = plant->phenology->sumDaysTotal(fi,startGrainFill);
   double growthRate = divide(dltDMPlant,nDays);
   return divide(growthRate, dmPerSeed);
   }
//------------------------------------------------------------------------------------------------
// Calculate the stress factor for diminishing potential harvest index
double Grain::yieldPartDemandStress(void)
   {
   double rueReduction = Min(Min(plant->getTempStress(),plant->nitrogen->getPhotoStress()),plant->phosphorus->getPhotoStress());
   return plant->water->photosynthesisStress() * rueReduction;
   }
//------------------------------------------------------------------------------------------------
// calculate daily grain dm demand using source / sink approach
double Grain::calcDMGrainSourceSink(void)
   {

   double totDMCaryopsis = divide(plant->biomass->getDltDM() , grainNo);
   totDMCaryopsis = divide(totDMCaryopsis, plant->phenology->getDltTTFM());
   potGFRate = (0.0000319 + 0.4026 * totDMCaryopsis) * 1000;     // in mg/grain/oCd
   potGFRate = Min(potGFRate, maxGFRate);
   return potGFRate * plant->phenology->getDltTTFM() * grainNo / 1000;   // in g/m2
   }
//------------------------------------------------------------------------------------------------
void Grain::RetranslocateN(double N)
   {
   dltNRetranslocate += N;
   }
//------------------------------------------------------------------------------------------------
double Grain::partitionDM(double dltDM)
   {
   dltDmGreen = Min(dltDMGrainDemand, dltDM);
   return Max(dltDmGreen,0.0);
   }
//------------------------------------------------------------------------------------------------
double Grain::grainDMDifferential(void)
   {
   return dltDMGrainDemand - dltDmGreen;
   }
//------------------------------------------------------------------------------------------------
double Grain::calcPDemand(void)
   {
   // Grain P demand   (demand from soil)
   pDemand = 0.0;
   return pDemand;
   }
//------------------------------------------------------------------------------------------------
double Grain::calcPRetransDemand(void)
   {
   // Grain P demand
   double maxP = pConcMax() * dmGreen;
   return Max(maxP - pGreen,0.0);
   }
//------------------------------------------------------------------------------------------------
void Grain::Summary(void)
   {
   char msg[120];
   sprintf(msg,"Stover (kg/ha)        = %.1f \t Grain yield (kg/ha)     = %.1f\n",
               plant->biomass->getAboveGroundBiomass() - dmGreen * 10.0, dmGreen * 10.0);
   scienceAPI.write(msg);
   sprintf(msg,"Grain %% water content = %.1f \t\t Grain yield wet (kg/ha) = %.1f\n",
               waterContent*100,dmGreen * 10.0 * 100 / (100 - waterContent*100));
   scienceAPI.write(msg);
   sprintf(msg,"Weight 1000 grains(g) = %.1f \t\t Grains/m^2              = %.1f\n",
         grainSize, grainNo);scienceAPI.write(msg);
   sprintf(msg,"Grains/head           = %.1f\n",grainNo / plant->getPlantDensity());
   scienceAPI.write(msg);
  }
//------------------------------------------------------------------------------------------------
void  Grain::Harvest(void)
   {
   // send crop_chopped
   if(dmGreen > 0)
      {
      BiomassRemovedType chopped;
      chopped.crop_type = plant->getCropType();

      double fracts[] = {0.0, 0.0, 0.0, 0.0, 0.0};  // No root or grain to residue.

      // Build surface residues by part
      for (unsigned part = 0; part < plant->PlantParts.size(); part++)
         {
         chopped.dm_type.push_back(plant->PlantParts[part]->getName());
         if(part < 4)
            {
            chopped.dlt_crop_dm.push_back(0.0);       // change in dry matter of crop (kg/ha)
            chopped.dlt_dm_n.push_back(0.0);          // N content of changed dry matter (kg/ha)
            chopped.dlt_dm_p.push_back(0.0);          // P content of changed dry matter (kg/ha)
            }
         else
            {
            chopped.dlt_crop_dm.push_back((float)((plant->PlantParts[part]->getDmGreen() +
                  plant->PlantParts[part]->getDmSenesced()) * gm2kg/sm2ha));
            chopped.dlt_dm_n.push_back((float)((plant->PlantParts[part]->getNGreen() +
                  plant->PlantParts[part]->getNSenesced()) * gm2kg/sm2ha));
            chopped.dlt_dm_p.push_back((float)((plant->PlantParts[part]->getPGreen() +
                  plant->PlantParts[part]->getPSenesced()) * gm2kg/sm2ha));
            }

         chopped.fraction_to_residue.push_back((float)fracts[part]);
         }

      scienceAPI.publish("BiomassRemoved", chopped);
      }
   initialize();
   }

