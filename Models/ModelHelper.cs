namespace CriticalCommonLib.Models
{
    public class ModelHelper {
        #region Fields
        public string ImcFileFormat { get; private set; }
        public byte ImcPartKey { get; private set; }
        public string ModelFileFormat { get; private set; }
        public byte VariantIndexWord { get; private set; }
        #endregion

        public ModelHelper(string imcFileFormat, byte imcPartKey, string modelFileFormat, byte variantIndexWord) {
            this.ImcFileFormat = imcFileFormat;
            this.ImcPartKey = imcPartKey;
            this.ModelFileFormat = modelFileFormat;
            this.VariantIndexWord = variantIndexWord;
        }
    }
}