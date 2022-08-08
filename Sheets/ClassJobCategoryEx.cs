using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class ClassJobCategoryEx : ClassJobCategory
    {
        private bool _isGathering;
        private bool _isCrafting;
        private bool _isCombat;

        public bool IsGathering => _isGathering;

        public bool IsCrafting => _isCrafting;

        public bool IsCombat => _isCombat;

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            //GLA,PGL,MRD,LNC,ARC,CNJ,THM,CRP,BSM,ARM,GSM,LTW,WVR,ALC,CUL,MIN,BTN,FSH,PLD,MNK,WAR,DRG,BRD,WHM,BLM,ACN,SMN,SCH,ROG,NIN,MCH,DRK,AST,SAM,RDM,BLU,GNB,DNC,RPR,SGE
            if (MIN || BTN || FSH)
            {
                _isGathering = true;
            }

            if (CRP || WVR || BLM || ALC || ARM || BSM || CUL || GSM || LTW)
            {
                _isCrafting = true;
            }

            if (GLA || PGL || MRD || LNC || ARC || CNJ || THM || PLD || MNK || WAR || DRG || BRD || WHM || BLM || ACN ||
                SMN || SCH || ROG || NIN || MCH || DRK || AST || SAM || RDM || BLU || GNB || DNC || RPR || SGE)
            {
                _isCombat = true;
            }
        }
    }
}