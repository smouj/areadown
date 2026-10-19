using System.Globalization;
using AreaDown.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AreaDown.Core.Tests;

[TestClass]
public sealed class TimestampParserTests
{
    [DataTestMethod]
    [DataRow("83", 83.0)]
    [DataRow("1:23", 83.0)]
    [DataRow("0:01:23", 83.0)]
    [DataRow("1.5", 1.5)]
    public void TryParse_AcceptsSupportedFormats(string input, double expectedSeconds)
    {
        Assert.IsTrue(TimestampParser.TryParse(input, out var result));
        Assert.AreEqual(TimeSpan.FromSeconds(expectedSeconds), result);
    }

    [TestMethod]
    public void TryParse_BlankInputMeansNoClipBound()
    {
        Assert.IsTrue(TimestampParser.TryParse("  ", out var result));
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryParse_AcceptsCurrentCultureDecimalSeparator()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");
            Assert.IsTrue(TimestampParser.TryParse("1,5", out var result));
            Assert.AreEqual(TimeSpan.FromSeconds(1.5), result);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [DataTestMethod]
    [DataRow("not a timestamp")]
    [DataRow("-1")]
    [DataRow("1:60")]
    public void TryParse_RejectsInvalidInput(string input)
    {
        Assert.IsFalse(TimestampParser.TryParse(input, out var result));
        Assert.IsNull(result);
    }
}
