using ProjNet.IO.Wkt.Tree;
using ProjNet.IO.Wkt.Utils;
using System;
using System.Linq;

using static ProjNet.IO.Wkt.Utils.Utils;
using Pidgin;
using static Pidgin.Parser;



namespace ProjNet.IO.Wkt.Core
{
    /// <summary>
    /// WKT1Parser - Base parser for WKT 1 using Parser Combinator Library Pidgin.
    /// </summary>
    /// <seealso href="https://github.com/benjamin-hodgson/Pidgin"/>
    public class WktCRS1Parser
    {

        // 6.3.1    Basic characters

        // <simple Latin upper case letter>
        internal static readonly Parser<char, char> SimpleLatinUpperCaseLetter = OneOf(
            Char('A'), Char('B'), Char('C'),
            Char('D'), Char('E'), Char('F'), Char('G'),
            Char('H'), Char('I'), Char('J'), Char('K'),
            Char('L'), Char('M'), Char('N'), Char('O'),
            Char('P'), Char('Q'), Char('R'), Char('S'),
            Char('T'), Char('U'), Char('V'), Char('W'),
            Char('X'), Char('Y'), Char('Z')
        );

        // <simple Latin lower case letter>
        internal static readonly Parser<char, char> SimpleLatinLowerCaseLetter = OneOf(
            Char('a'), Char('b'), Char('c'),
            Char('d'), Char('e'), Char('f'), Char('g'),
            Char('h'), Char('i'), Char('j'), Char('k'),
            Char('l'), Char('m'), Char('n'), Char('o'),
            Char('p'), Char('q'), Char('r'), Char('s'),
            Char('t'), Char('u'), Char('v'), Char('w'),
            Char('x'), Char('y'), Char('z')
        );

        // <digit>
        internal static readonly Parser<char, char> Didgit = OneOf(
            Char('0'), Char('1'), Char('2'),
            Char('3'), Char('4'), Char('5'),
            Char('6'), Char('7'), Char('8'),
            Char('9')
        );

        // <space>
        internal static readonly Parser<char, char> Space = Char(' ');

        // <double quote>
        internal static readonly Parser<char, char> DoubleQuote = Char('"');

        // <number sign>
        internal static readonly Parser<char, char> NumberSign = Char('#');

        // <percent>
        internal static readonly Parser<char, char> Percent = Char('%');

        // <ampersand>
        internal static readonly Parser<char, char> Ampersand = Char('&');

        // <quote>
        internal static readonly Parser<char, char> Quote = Char('\'');

        // <left paren>
        internal static readonly Parser<char, char> LeftParen = Char('(');
        // <right paren>
        internal static readonly Parser<char, char> RightParen = Char(')');

        // <asterisk>
        internal static readonly Parser<char, char> Asterisk = Char('*');

        // <plus sign>
        internal static readonly Parser<char, char> PlusSign = Char('+');

        // <comma>
        internal static readonly Parser<char, char> Comma = Char(',');

        // <minus sign> ::- <hyphen>
        internal static readonly Parser<char, char> MinusSign = Char('-');
        internal static readonly Parser<char, char> Hyphen = Char('-');

        // <period>
        internal static readonly Parser<char, char> Period = Char('.');

        // <solidus>
        internal static readonly Parser<char, char> Solidus = Char('/');
        // <reverse solidus>
        internal static readonly Parser<char, char> ReverseSolidus = Char('\\');

        // <colon>
        internal static readonly Parser<char, char> Colon = Char(':');
        // <semicolon>
        internal static readonly Parser<char, char> SemiColon = Char(';');

        // <less than operator>
        internal static readonly Parser<char, char> LessThanOperator = Char('<');
        // <equals operator>
        internal static readonly Parser<char, char> EqualsOperator = Char('=');
        // <greater than operator>
        internal static readonly Parser<char, char> GreaterThanOperator = Char('>');

        // <question mark>
        internal static readonly Parser<char, char> QuestionMark = Char('?');

        // <left bracket>
        internal static readonly Parser<char, char> LeftBracket = Char('[');
        // <right bracket>
        internal static readonly Parser<char, char> RightBracket = Char(']');

        // <circumflex>
        internal static readonly Parser<char, char> Circumflex = Char('^');

        // <underscore>
        internal static readonly Parser<char, char> Underscore = Char('_');

        // <left brace>
        internal static readonly Parser<char, char> LeftBrace = Char('{');
        // <right brace>
        internal static readonly Parser<char, char> RightBrace = Char('}');

        // <vertical bar>
        internal static readonly Parser<char, char> VerticalBar = Char('|');

        // <degree symbol>
        internal static readonly Parser<char, char> DegreeSymbol = Char('\u00B0');


                // 6.3.2    Numbers

        // <sign>
        internal static readonly Parser<char, char> Sign = OneOf(PlusSign, MinusSign);
        // <unsigned integer>
        internal static readonly Parser<char, string> UnsignedIntegerString =
            WktCRS1Parser.Didgit.AtLeastOnceString();
        internal static readonly Parser<char, uint> UnsignedInteger =
            UnsignedIntegerString
                .Select(uint.Parse);

        // <exact numeric literal>
        internal static readonly Parser<char, double> ExactNumericLiteralDotted =
            Period
                .Then(UnsignedIntegerString, (c, ui) => CalcAsFractionOf(0, ui));

        internal static readonly Parser<char, double> ExactNumericLiteral =
            UnsignedInteger.Optional()
                .Then(ExactNumericLiteralDotted.Optional(), (ui, d) => ui.GetValueOrDefault() + d.GetValueOrDefault());


        // <signed integer>
        internal static readonly Parser<char, int> SignedInteger =
            Sign.Optional()
                .Then(UnsignedInteger, (c, ui) => (int) ((c.HasValue && c.Value == '-' ? -1 : 1) * ui));

        // <exponent>
        internal static readonly Parser<char, int> Exponent =
            SignedInteger;

        // <mantissa>
        internal static readonly Parser<char, double> Mantissa =
            ExactNumericLiteral;

        // <approximate numeric literal>
        internal static readonly Parser<char, double> ApproximateNumericLiteralExp =
                OneOf(Char('e'),Char('E'))
                .Then(Exponent)
                .Select(e => Math.Pow(10, e));
        internal static readonly Parser<char, double> ApproximateNumericLiteral =
            Mantissa.Then(ApproximateNumericLiteralExp.Optional(), (m, e) => m * (e.HasValue ? e.Value : 1));

        // <unsigned numeric literal>
        internal static readonly Parser<char, double> UnsignedNumericLiteral = OneOf(
            ApproximateNumericLiteral,
            ExactNumericLiteral
        );

        // <signed numeric literal>
        internal static readonly Parser<char, double> SignedNumericLiteral =
            Sign.Optional()
                .Then(UnsignedNumericLiteral, (s, d) => (double) ((s.HasValue && s.Value == '-' ? -1 : 1) * d));


        // <number>
        internal static readonly Parser<char, double> Number = OneOf(
            SignedNumericLiteral,
            UnsignedNumericLiteral)
            .Labelled("number");


        // 6.3.3 Date and time

        // <day> ::= <unsigned integer> !! two digits
        internal static readonly Parser<char, uint> Day = WktCRS1Parser.Didgit
            .Repeat(2)
            .Select(d => uint.Parse(new string(d.ToArray())));
        // <month> ::= <unsigned integer> !! two digits
        internal static readonly Parser<char, uint> Month = WktCRS1Parser.Didgit
            .Repeat(2)
            .Select(m => uint.Parse(new string(m.ToArray())));
        // <year> ::= <unsigned integer> !! four digits
        internal static readonly Parser<char, uint> Year = WktCRS1Parser.Didgit
            .Repeat(4)
            .Select(y => uint.Parse(new string(y.ToArray())));

        // <hour> ::= <unsigned integer>
        // !! two digits including leading zero if less than 10
        internal static readonly Parser<char, uint> Hour = WktCRS1Parser.Didgit
            .Repeat(2)
            .Select(h => uint.Parse(new string(h.ToArray())));

        // <minute> ::= <unsigned integer>
        // !! two digits including leading zero if less than 10
        internal static readonly Parser<char, uint> Minute = WktCRS1Parser.Didgit
            .Repeat(2)
            .Select(h => uint.Parse(new string(h.ToArray())));

        // <seconds integer> ::= <unsigned integer>
        // !! two digits including leading zero if less than 10
        internal static readonly Parser<char, uint> SecondsInteger = WktCRS1Parser.Didgit
            .Repeat(2)
            .Select(h => uint.Parse(new string(h.ToArray())));
        // <seconds fraction> ::= <unsigned integer>
        internal static readonly Parser<char, uint> SecondsFraction = UnsignedInteger;

        // <second> ::= <seconds integer> [ <period> [ <seconds fraction> ] ]
        // !! In this International Standard the separator between the integer and fractional parts of a second value shall be a period. The ISO 8601 preference for comma is not permitted.
        internal static readonly Parser<char, uint> SecondsDotted = Period.Then(SecondsFraction);

        internal static readonly Parser<char, DateTimeBuilder> Second = SecondsInteger.Then(SecondsDotted.Optional(),
            (u, mf) => new DateTimeBuilder()
                .SetSeconds((int) u)
                .SetMilliseconds((int) mf.GetValueOrDefault()));

        // <utc designator> ::= Z
        internal static readonly Parser<char, char> UtcDesignator = Char('Z');

        // <local time zone designator> ::= { <plus sign> | <minus sign> } <hour> [ <colon> <minute> ]
        internal static readonly Parser<char, uint> ColonMinute = Colon.Then(Minute);
        internal static readonly Parser<char, TimeSpan> LocalTimeZoneDesignator = Sign
            .Then(Hour, (ms, u) => (ms == '-' ? -1 : 1) * u)
            .Then(ColonMinute.Optional(), (h, mm) => new TimeSpan(0, (int) h, (int) mm.GetValueOrDefault(), 0));

        // <time zone designator> ::= <utc designator> | <local time zone designator>
        internal static readonly Parser<char, TimeSpan> TimeZoneDesignator = OneOf(
            UtcDesignator.Select(z => TimeSpan.Zero),
            LocalTimeZoneDesignator
        );

        // <time designator> ::= T
        internal static readonly Parser<char, char> TimeDesignator = Char('T');

        // <24 hour clock> ::= <time designator> <hour> [ <colon> <minute> [ <colon> <second> ] ] <time zone designator>
        internal static readonly Parser<char, DateTimeBuilder> ColonSecond = Colon.Then(Second);

        internal static readonly Parser<char, DateTimeBuilder> _24HourClock = TimeDesignator
            .Then(Hour, (c, h) => new DateTimeBuilder().SetHour((int) h))
            .Then(ColonMinute.Optional(), (dtb, mm) => dtb.SetMinutes((int) mm.GetValueOrDefault()))
            .Then(ColonSecond.Optional(),
                (dtb1, dtb2) =>
                    dtb2.HasValue
                        ? dtb1.SetSeconds((int) dtb2.GetValueOrDefault().Seconds)
                            .SetMilliseconds((int) dtb2.GetValueOrDefault().Milliseconds)
                        : dtb1)
            .Then(TimeZoneDesignator, (dtb, ts) => dtb.SetLocalOffset(ts));

        // <ordinal day> ::= <unsigned integer> !! three digits
        internal static readonly Parser<char, uint> OrdinalDay = WktCRS1Parser.Didgit
            .Repeat(3)
            .Select(od => uint.Parse(new string(od.ToArray())));

        // <Gregorian ordinal date> ::= <year> [ <hyphen> <ordinal day> ]
        internal static readonly Parser<char, uint> GregorianOrdinalDateOptionalPart = Hyphen
            .Then(OrdinalDay);
        internal static readonly Parser<char, DateTimeBuilder> GregorianOrdinalDate = Year
            .Then(GregorianOrdinalDateOptionalPart.Optional(),
                (y, md) => new DateTimeBuilder().SetYear((int) y).SetOrdinalDay(md));

        // <Gregorian ordinal datetime> ::= <Gregorian ordinal date> [ <24 hour clock> ]
        internal static readonly Parser<char, DateTimeBuilder> GregorianOrdinalDateTime = GregorianOrdinalDate
            .Then(_24HourClock.Optional(),
                (dtb, mdtb) =>
                    mdtb.HasValue ?
                        dtb.Merge(mdtb.GetValueOrDefault())
                        : dtb);


        // <Gregorian calendar date> ::= <year> [ <hyphen> <month> [ <hyphen> <day> ] ]
        internal static readonly Parser<char, DateTimeBuilder> GregorianCalendarDateHyphenDay = Hyphen
            .Then(Day, (hh, d) => new DateTimeBuilder().SetDay((int)d));

        internal static readonly Parser<char, DateTimeBuilder> GregorianCalendarDateHyphenMonth = Hyphen
            .Then(Month, (hh, m) => new DateTimeBuilder().SetMonth((int) m))
            .Then(GregorianCalendarDateHyphenDay.Optional(),
                (dtbm, dtbd) => dtbm.SetDay((int) dtbd.GetValueOrDefault().Day));

        internal static readonly Parser<char, DateTimeBuilder> GregorianCalendarDate = Year
            .Then(GregorianCalendarDateHyphenMonth.Optional(), (y, dtb) =>
                dtb.HasValue ? dtb.GetValueOrDefault().SetYear((int) y) : new DateTimeBuilder().SetYear((int) y));

        // <Gregorian calendar datetime> ::= <Gregorian calendar date> [ <24 hours clock>]
        internal static readonly Parser<char, DateTimeBuilder> GregorianCalendarDateTime =
            GregorianCalendarDate.Then(_24HourClock.Optional(),
                (dtbdate, mdtbc) => mdtbc.HasValue ? dtbdate.Merge(mdtbc.Value) : dtbdate);

        // <datetime> ::= <Gregorian calendar datetime> | <Gregorian ordinal datetime>
        internal static readonly Parser<char, DateTimeOffset> DateTimeParser = GregorianCalendarDateTime
            .Or(GregorianOrdinalDateTime)
            .Select(dtb => dtb.ToDateTimeOffset());


        // *** 6.3.4 CRS WKT characters ***

        // <doublequote symbol> ::= "" !! two double quote chars
        internal static readonly Parser<char, string> DoubleQuoteSymbol = Try(
            DoubleQuote.Repeat(2).Select(c => new string(c.ToArray())));

        // <nondoublequote character> ::=  !! A <nondoublequote character> is any character of the source language character set other than a <double quote>.
        internal static readonly Parser<char, string> NonDoubleQuoteCharacter =
            DoubleQuoteSymbol.Where(s => !DoubleQuote.Parse(s).Success);

        // <left delimiter> ::= <left bracket> | <left paren>
        // !! In this International Standard the preferred left delimiter is <left bracket>. <left paren> is permitted for backward compatibility. Implementations shall be able to read both forms.
        internal static readonly Parser<char, char> LeftDelimiter = LeftBracket.Or(LeftParen);

        // <right delimiter> ::= <right bracket> | <right paren>
        internal static readonly Parser<char, char> RightDelimiter = RightBracket.Or(RightParen);

        // <wkt separator> ::= <comma>
        internal static readonly Parser<char, char> WktSeparator = Comma;

        // <wkt Latin text character> ::= <simple Latin upper case letter> | <simple Latin lower case letter> | <digit> | <underscore>
        // | <left bracket> | <right bracket> | <left paren> | <right paren>
        // | <left brace> | <right brace>
        // | <less than operator> | <equals operator> | <greater than operator> | <period> | <comma> | <colon> | <semicolon>
        // | <plus sign> | <minus sign> | <space> | <number sign>
        // | <percent> | <ampersand> | <quote> | <asterisk> | <circumflex>
        // | <solidus> | <reverse solidus> | <question mark> | <vertical bar>
        // | <degree symbol> | <doublequote symbol>
        internal static readonly Parser<char, char> WktLatinTextCharacterChars = OneOf(
            SimpleLatinUpperCaseLetter, SimpleLatinLowerCaseLetter, WktCRS1Parser.Didgit, Underscore,
            LeftBracket, RightBracket,
            LessThanOperator, EqualsOperator, GreaterThanOperator, Period, Comma, Colon, SemiColon,
            PlusSign, MinusSign, Space, NumberSign,
            Percent, Ampersand, Quote, Asterisk, Circumflex,
            Solidus, ReverseSolidus, QuestionMark, VerticalBar,
            DegreeSymbol
        );
        internal static readonly Parser<char, string> WktLatinTextCharacter =
            DoubleQuoteSymbol
            .Or(WktLatinTextCharacterChars.Select(c => c.ToString()))
            .Labelled("Wkt Latin Text Character");
        internal static readonly Parser<char, string>
            WktLatinTextCharacters = WktLatinTextCharacter.AtLeastOnceString();

        // <quoted Latin text> ::= <double quote> <wkt Latin text character>... <double quote>
        internal static readonly Parser<char, string> QuotedLatinText =
            WktLatinTextCharacters.Between(DoubleQuote, DoubleQuote)
                .Labelled("Quoted Latin Text");

        // <wkt Unicode text character> ::= <nondouble quote> | <doublequote symbol>
        internal static readonly Parser<char, string> WktUnicodeTextCharacter = OneOf(
            LetterOrDigit.Select(c => c.ToString()),
            Space.Select(c => c.ToString()),
            DoubleQuoteSymbol)
                .Labelled("Wkt Unicode Text Character");
        internal static readonly Parser<char, string>
            WktUnicodeTextCharacters = WktUnicodeTextCharacter.AtLeastOnceString();

        internal static readonly Parser<char, string> QuotedUnicodeText =
            WktUnicodeTextCharacters.Between(DoubleQuote, DoubleQuote)
                .Labelled("Quoted Unicode Text");


        // 7.3.2 ScopeParser

        // <scope text description> ::= <quoted Latin text>
        internal static readonly Parser<char, string> ScopeTextDescriptionParser = QuotedLatinText;

        // <scope keyword> ::= SCOPE
        internal static readonly Parser<char, string> ScopeKeywordParser = String("SCOPE");


        // <scope> ::= <scope keyword> <left delimiter> <scope text description> <right delimiter>
        internal static readonly Parser<char, WktScope> ScopeParser = ScopeKeywordParser
            .Then(LeftDelimiter)
            .Then(ScopeTextDescriptionParser, (c, s) =>  new WktScope{Description = s})
            .Before(RightDelimiter)
            .Select(std => std);


        //    7.3.3.2 Area description

        // <area text description> ::= <quoted Latin text>
        internal static readonly Parser<char, string> AreaTextDescriptionParser = QuotedLatinText;

        // <area description keyword> ::= AREA
        internal static readonly Parser<char, string> AreaDescriptionKeywordParser = String("AREA");

        // <area description> ::= <area description keyword> <left delimiter> <area text description> <right delimiter>
        internal static readonly Parser<char, AreaDescription> AreaDescriptionParser = AreaDescriptionKeywordParser
            .Then(LeftDelimiter)
            .Then(AreaTextDescriptionParser, (c, s) => new AreaDescription {Description = s})
            .Before(RightDelimiter);

        // 7.3.3.3 Geographic bounding box

        //  <geographic bounding box keyword> ::= BBOX
        internal static readonly Parser<char, string> GeographicBoundingBoxKeywordParser = String("BBOX");

        // <lower left latitude> ::= <number>
        internal static readonly Parser<char, double> LowerLeftLatitudeParser = Number;
        // <lower left longitude> ::= <number>
        internal static readonly Parser<char, double> LowerLeftLongitudeParser = Number;

        // <upper right latitude> ::= <number>
        internal static readonly Parser<char, double> UpperRightLatitudeParser = Number;
        // <upper right longitude> ::= <number>
        internal static readonly Parser<char, double> UpperRightLongitudeParser = Number;

        // <geographic bounding box> ::= <geographic bounding box keyword> <left delimiter> <lower left latitude> <wkt separator> <lower left longitude> <wkt separator> <upper right latitude> <wkt separator> <upper right longitude> <right delimiter>
        internal static readonly Parser<char, BBox> GeographicBoundingBoxParser = GeographicBoundingBoxKeywordParser
            .Then(LeftDelimiter)
            .Then(LowerLeftLatitudeParser, (c, d) => new BBox{LowerLeftLatitude = d} ).Before(WktSeparator)
            .Then(LowerLeftLongitudeParser, (bbox, dLong) =>
            {
                bbox.LowerLeftLongitude = dLong;
                return bbox;
            }).Before(WktSeparator)
            .Then(UpperRightLatitudeParser, (bbox, dLat) => { bbox.UpperRightLatitude = dLat;
                return bbox;
            }).Before(WktSeparator)
            .Then(UpperRightLongitudeParser, (bbox, dLong) => { bbox.UpperRightLongitude = dLong;
                return bbox;
            })
            .Before(RightDelimiter);


        // 7.3.3.4 Vertical extent

        // <vertical extent minimum height> ::= <number>
        internal static readonly Parser<char, double> VerticalExtentMinimumHeightParser = Number;
        // <vertical extent maximum height> ::= <number>
        internal static readonly Parser<char, double> VerticalExtentMaximumHeightParser = Number;
        // <vertical extent keyword>        ::= VERTICALEXTENT
        internal static readonly Parser<char, string> VerticalExtentKeywordParser = String("VERTICALEXTENT");

        // <vertical extent> ::= <vertical extent keyword> <left delimiter> <vertical extent minimum height> <wkt separator> <vertical extent maximum height>
        // [ <wkt separator> <length unit> ] <right delimiter>
        internal static readonly Parser<char, WktVerticalExtent> VerticalExtentParser = VerticalExtentKeywordParser
            .Then(LeftDelimiter)
            .Then(VerticalExtentMinimumHeightParser).Before(WktSeparator)
            .Then(VerticalExtentMaximumHeightParser,
                (min, max) => new WktVerticalExtent {MinimumHeight = min, MaximumHeight = max})
            // TODO: Add Optional LengthUnit support!
            .Before(RightDelimiter);


        // 7.3.3.5 Temporal extent

        // <temporal extent keyword> ::= TIMEEXTENT
        internal static readonly Parser<char, string> TemporalExtentKeywordParser = String("TIMEEXTENT");

        // <temporal extent start> ::= <datetime> | <quoted Latin text>
        internal static readonly Parser<char, (DateTimeOffset?, string)> TemporalExtentStartParser =
            DateTimeParser.Select(x => new ValueTuple<DateTimeOffset?, string>(x, null))
                .Or(QuotedLatinText.Select(x => new ValueTuple<DateTimeOffset?, string>(null, x)));
        // <temporal extent end> ::= <datetime> | <quoted Latin text>
        internal static readonly Parser<char, (DateTimeOffset?, string)> TemporalExtentEndParser =
            DateTimeParser.Select(x => new ValueTuple<DateTimeOffset?, string>(x, null))
                .Or(QuotedLatinText.Select(x => new ValueTuple<DateTimeOffset?, string>(null, x)));

        // <temporal extent> ::= <temporal extent keyword> <left delimiter>
        //                          <temporal extent start> <wkt separator> <temporal extent end> <right delimiter>
        internal static readonly Parser<char, WktTemporalExtent> TemporalExtentParser =
            TemporalExtentKeywordParser
                .Then(LeftDelimiter)
                .Then(TemporalExtentStartParser,
                    (c, tuple) => new WktTemporalExtent {StartDateTime = tuple.Item1, StartText = tuple.Item2})
                .Before(WktSeparator)
                .Then(TemporalExtentEndParser, (extent, tuple) =>
                {
                    extent.EndDateTime = tuple.Item1;
                    extent.EndText = tuple.Item2;
                    return extent;
                })
                .Before(RightDelimiter);

        // <extent> ::= <area description> | <geographic bounding box> | <vertical extent> | <temporal extent>
        // !! Constraint: each extent type shall have a maximum occurrence of 1.
        internal static readonly Parser<char, WktExtent> ExtentParser = OneOf(
            AreaDescriptionParser.Cast<WktExtent>(),
            GeographicBoundingBoxParser.Cast<WktExtent>(),
            VerticalExtentParser.Cast<WktExtent>(),
            TemporalExtentParser.Cast<WktExtent>()
        );


        // 7.3.4 Identifier

        // <uri> ::= <quoted Latin text>
        internal static readonly Parser<char, string> UriParser = QuotedLatinText;
        // <uri keyword> ::= URI
        internal static readonly Parser<char, string> UriKeywordParser = String("URI");
        // <id uri> ::= <uri keyword> <left delimiter> <uri> <right delimiter>
        internal static readonly Parser<char, WktUri> IdUriParser = UriKeywordParser
            .Then(LeftDelimiter)
            .Then(UriParser)
            .Before(RightDelimiter)
            .Select(s => new WktUri(s));

        // <citation> ::= <quoted Latin text>
        internal static readonly Parser<char, string> CitationParser = QuotedLatinText;
        // <citation keyword> ::= CITATION
        internal static readonly Parser<char, string> CitationKeywordParser = String("CITATION");
        // <authority citation> ::= <citation keyword> <left delimiter> <citation> <right delimiter>
        internal static readonly Parser<char, WktAuthorityCitation> AuthorityCitationParser = CitationKeywordParser
            .Then(LeftDelimiter)
            .Then(CitationParser)
            .Before(RightDelimiter)
            .Select(s => new WktAuthorityCitation(s));

        // <version> ::= <number> | <quoted Latin text>
        internal static readonly Parser<char, object> VersionParser = OneOf(
            Try(QuotedLatinText.Cast<object>()),
            Number.Cast<object>());

        // <authority unique identifier> ::= <number> | <quoted Latin text>
        internal static readonly Parser<char, object> AuthorityUniqueIdentifierParser = OneOf(
            Try(QuotedLatinText.Cast<object>()),
            Number.Cast<object>());
        // <authority name> ::= <quoted Latin text>
        internal static readonly Parser<char, string> AuthorityNameParser = QuotedLatinText;
        // <identifier keyword> ::= ID
        internal static readonly Parser<char, string> IdentifierKeywordParser = String("ID");
        // <identifier> ::= <identifier keyword> <left delimiter> <authority name>
        // <wkt separator> <authority unique identifier>
        // [ <wkt separator> <version> ] [ <wkt separator> <authority citation> ] [ <wkt separator> <id uri> ] <right delimiter>
        internal static readonly Parser<char, WktIdentifier> IdentifierParser = Try(IdentifierKeywordParser
            .Then(LeftDelimiter)
            .Then(AuthorityNameParser).Select(an => new WktIdentifier {AuthorityName = an})
            .Then(WktSeparator.Then(AuthorityUniqueIdentifierParser), (identifier, o) =>
            {
                identifier.AuthorityUniqueIdentifier = o;
                return identifier;
            })
            .Then(Try(WktSeparator.Then(VersionParser)).Optional(), (identifier, version) =>
            {
                identifier.Version = version.GetValueOrDefault();
                return identifier;
            })
            .Then(Try(WktSeparator.Then(AuthorityCitationParser)).Optional(), (identifier, citation) =>
            {
                identifier.AuthorityCitation = citation.GetValueOrDefault();
                return identifier;
            })
            .Then(Try(WktSeparator.Then(IdUriParser)).Optional(), (identifier, uri) =>
            {
                identifier.IdUri = uri.GetValueOrDefault();
                return identifier;
            })
            .Before(RightDelimiter));


        // 7.3.5 Remark
        // <remark keyword> ::= REMARK
        internal static readonly Parser<char, string> RemarkKeywordParser = String("REMARK");

        // <remark> ::= <remark keyword> <left delimiter> <quoted Unicode text> <right delimiter>
        internal static readonly Parser<char, WktRemark> RemarkParser = RemarkKeywordParser
            .Then(LeftDelimiter)
            .Then(QuotedUnicodeText)
            .Before(RightDelimiter)
            .Select(s => new WktRemark(s));


        // <scope extent identifier remark> ::= [ <wkt separator> <scope> ]
        //                                      [ { <wkt separator> <extent> } ]...
        //                                      [ { <wkt separator> <identifier> } ]...
        //                                      [ <wkt separator> <remark>]
        internal static readonly Parser<char, WktScopeExtentIdentifierRemarkElement>
            ScopeExtentIdentifierRemarkElementParser =
                WktSeparator.Then(ScopeParser).Optional()
                    .Then(WktSeparator.Then(ExtentParser).Many(),
                        (scope, extents) =>
                            new WktScopeExtentIdentifierRemarkElement(scope.GetValueOrDefault(), extents.ToList()))
                    .Then(WktSeparator.Then(IdentifierParser).Many(),
                        (element, identifiers) =>
                        {
                            element.Identifiers = identifiers.ToList();
                            return element;
                        })
                    .Then(WktSeparator.Then(RemarkParser).Optional(), (element, remark) =>
                    {
                        element.Remark = remark.GetValueOrDefault();
                        return element;
                    });

    }
}

