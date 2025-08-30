using LibClaimsDataImport.Importer;

namespace LibClaimsDataImport.Tests
{
    public class DataParseTests
    {
        [Theory]
        [InlineData("00123", typeof(string))]                      // leading zero, length > 1 → string
        [InlineData("0", typeof(int))]                             // single zero → integer
        [InlineData("00", typeof(string))]                         // multiple zeros → string (leading-zero rule)
        [InlineData(" 0123", typeof(string))]                      // leading space + leading zero → string
        [InlineData("12345678901234", typeof(long))]               // 14 digits → integer (long)
        [InlineData("123456789012345", typeof(long))]              // 15 digits → integer (long)
        [InlineData("0123456789", typeof(string))]                 // leading zero in digits-only → string
        [InlineData("9,223,372,036,854,775,808", typeof(decimal))] // > long.MaxValue ⇒ next (decimal)
        [InlineData("19,223,372,036,854,775,808", typeof(decimal))]// much larger ⇒ decimal
        [InlineData("09,223,372,036,854,775,808", typeof(string))] // leading zero ⇒ string
        [InlineData("2253402054095601", typeof(long))]             // 16 digits, no leading zero ⇒ long
        public void DetectType_FollowsClassificationRules(string input, Type expected)
        {
            Type detected = DataTypeDetector.DetectType(input);
            Assert.Equal(expected, detected);
        }
    }

}
