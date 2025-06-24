using NUnit.Framework;
using Pidgin;
using ProjNet.IO.Wkt.Core;

namespace ProjNet.IO.Wkt.Tests.Core;

public class WktParserTests
{

    [Test]
    public void TestUnsignedIntegerParser()
    {
        // Arrange
        string parserText01 = $@"210677";

        // Act
        uint parserResult01 = WktParser.UnsignedIntegerParser.ParseOrThrow(parserText01);

        // Assert
        Assert.That(parserResult01, Is.EqualTo(210677));
    }

    [Test]
    public void TestSignedIntegerParser()
    {
        // Arrange
        string parserText01 = $@"+210677";
        string parserText02 = $@"-210677";
        string parserText03 = $@"210677";

        // Act
        int parserResult01 = WktParser.SignedIntegerParser.ParseOrThrow(parserText01);
        int parserResult02 = WktParser.SignedIntegerParser.ParseOrThrow(parserText02);
        int parserResult03 = WktParser.SignedIntegerParser.ParseOrThrow(parserText03);

        // Assert
        Assert.That(parserResult01, Is.EqualTo(210677));
        Assert.That(parserResult02, Is.EqualTo(-210677));
        Assert.That(parserResult03, Is.EqualTo(210677));
    }


    [Test]
    public void TestSignedNumericLiteralParser()
    {
        // Arrange
        string parserText01 = "-100.333333333333";
        string parserText02 = "+100.333333333333";
        string parserText03 = "100.333333333333";
        string parserText04 = ".333333333333";

        // Act
        double parserResult01 = WktParser.SignedNumericLiteralParser.ParseOrThrow(parserText01);
        double parserResult02 = WktParser.SignedNumericLiteralParser.ParseOrThrow(parserText02);
        double parserResult03 = WktParser.SignedNumericLiteralParser.ParseOrThrow(parserText03);
        double parserResult04 = WktParser.SignedNumericLiteralParser.ParseOrThrow(parserText04);

        // Assert
        Assert.That(parserResult01, Is.EqualTo(-100.333333333333));
        Assert.That(parserResult02, Is.EqualTo(100.333333333333));
        Assert.That(parserResult03, Is.EqualTo(100.333333333333));
        Assert.That(parserResult04, Is.EqualTo(0.333333333333));
    }

    [Test]
    public void TestExactNumericLiteralParser()
    {
        // Arrange
        string parserText01 = $@"21.043";
        string parserText02 = $@"0.043";
        string parserText03 = $@".043";

        // Act
        double parserResult01 = WktParser.ExactNumericLiteralParser.ParseOrThrow(parserText01);
        double parserResult02 = WktParser.ExactNumericLiteralParser.ParseOrThrow(parserText02);
        double parserResult03 = WktParser.ExactNumericLiteralParser.ParseOrThrow(parserText03);

        // Assert
        Assert.That(parserResult01, Is.EqualTo(21.043d));
        Assert.That(parserResult02, Is.EqualTo(0.043d));
        Assert.That(parserResult03, Is.EqualTo(0.043d));
    }

    [Test]
    public void TestApproximateNumericLiteralParser()
    {
        // Arrange
        string parserText01 = $@"21.04E-3";
        string parserText02= $@"21.04E+3";
        string parserText03 = $@"21.04E3";
        string parserText04 = $@"0.04E3";
        string parserText05 = $@".04E3";

        // Act
        double parserResult01 = WktParser.ApproximateNumericLiteralParser.ParseOrThrow(parserText01);
        double parserResult02 = WktParser.ApproximateNumericLiteralParser.ParseOrThrow(parserText02);
        double parserResult03 = WktParser.ApproximateNumericLiteralParser.ParseOrThrow(parserText03);
        double parserResult04 = WktParser.ApproximateNumericLiteralParser.ParseOrThrow(parserText04);
        double parserResult05 = WktParser.ApproximateNumericLiteralParser.ParseOrThrow(parserText05);

        // Assert
        Assert.That(parserResult01, Is.EqualTo(0.02104d));
        Assert.That(parserResult02, Is.EqualTo(21040d));
        Assert.That(parserResult03, Is.EqualTo(21040d));
        Assert.That(parserResult04, Is.EqualTo(40d));
        Assert.That(parserResult05, Is.EqualTo(40d));
    }

    [Test]
    public void TestQuotedNameParser()
    {
        // Arrange
        string str01 = "MyTextString";
        string parserText01 = $"\"{str01}\"";

        // Act
        string parserResult01 = WktParser.QuotedNameParser.ParseOrThrow(parserText01);

        // Assert
        Assert.That(parserResult01, Is.EqualTo(str01));
    }


}
