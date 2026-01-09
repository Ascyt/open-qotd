using static OpenQotd.Core.Configs.Commands.Helpers.Validity;

namespace Tests.Commands
{
    public class ConfigCommandHexValidationTests
    {
        [Theory]
        [InlineData("#8acfac")]
        [InlineData("#000000")]
        [InlineData("#FFFFFF")]
        [InlineData("#abcdef")]
        [InlineData("#123456")]
        public void IsValidHexCode_ValidHexWithHashPrefix_ReturnsTrue(string hexCode)
        {
            string input = hexCode;
            bool result = IsValidHexCode(ref input);

            Assert.True(result);
            Assert.StartsWith("#", input);
            Assert.Equal(7, input.Length);
        }

        [Theory]
        [InlineData("8acfac")]
        [InlineData("000000")]
        [InlineData("FFFFFF")]
        [InlineData("abcdef")]
        [InlineData("123456")]
        public void IsValidHexCode_ValidHexWithoutHashPrefix_ReturnsTrue(string hexCode)
        {
            string input = hexCode;
            bool result = OpenQotd.Core.Configs.Commands.Helpers.Validity.IsValidHexCode(ref input);

            Assert.True(result);
            Assert.StartsWith("#", input);
            Assert.Equal(7, input.Length);
        }

        [Theory]
        [InlineData("ABC123", "#abc123")]
        [InlineData("ABCDEF", "#abcdef")]
        [InlineData("#ABC123", "#abc123")]
        [InlineData("#ABCDEF", "#abcdef")]
        public void IsValidHexCode_UppercaseInput_ConvertsToLowercase(string input, string expected)
        {
            string hexCode = input;
            bool result = IsValidHexCode(ref hexCode);

            Assert.True(result);
            Assert.Equal(expected, hexCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("#")]
        [InlineData("#12345")]
        [InlineData("#1234567")]
        [InlineData("12345")]
        [InlineData("1234567")]
        public void IsValidHexCode_WrongLength_ReturnsFalse(string hexCode)
        {
            string input = hexCode;
            bool result = IsValidHexCode(ref input);

            Assert.False(result);
        }

        [Theory]
        [InlineData("#gggggg")]
        [InlineData("#12345g")]
        [InlineData("#zzzzzz")]
        [InlineData("gggggg")]
        [InlineData("#12-456")]
        [InlineData("#12 456")]
        public void IsValidHexCode_InvalidCharacters_ReturnsFalse(string hexCode)
        {
            string input = hexCode;
            bool result = IsValidHexCode(ref input);

            Assert.False(result);
        }

        [Theory]
        [InlineData("##12345")]
        [InlineData("###1234")]
        public void IsValidHexCode_MultipleHashPrefixes_ReturnsFalse(string hexCode)
        {
            string input = hexCode;
            bool result = IsValidHexCode(ref input);

            Assert.False(result);
        }

        [Fact]
        public void IsValidHexCode_MixedCase_NormalizesToLowercase()
        {
            string input = "AaBbCc";
            bool result = IsValidHexCode(ref input);

            Assert.True(result);
            Assert.Equal("#aabbcc", input);
        }

        [Theory]
        [InlineData("#aAbBcC")]
        [InlineData("aAbBcC")]
        public void IsValidHexCode_MixedCaseWithAndWithoutHash_NormalizesToLowercase(string hexCode)
        {
            string input = hexCode;
            bool result = IsValidHexCode(ref input);

            Assert.True(result);
            Assert.Equal("#aabbcc", input);
        }
    }
}
