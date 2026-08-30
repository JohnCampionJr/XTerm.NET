using XTerm.Parser;
using XTerm.Events.Parser;

namespace XTerm.Tests.Parser;

[TestClass]

public class ParserDebugTests
{
    [TestMethod]
    public void Debug_Parse_5A()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var calls = new List<(string, Params)>();
        parser.Csi += (sender, e) => {
            var paramsClone = e.Parameters.Clone();
            calls.Add((e.Identifier, paramsClone));
        };

        // Act
        parser.Parse("\x1B[5A");

        // Assert
        calls.Should().ContainSingle();
        var call = calls[0];
        call.Item1.Should().Contain("A");
        
        // Debug output
        Console.WriteLine($"Params Length: {call.Item2.Length}");
        for (int i = 0; i < call.Item2.Length; i++)
        {
            Console.WriteLine($"  Param[{i}] = {call.Item2.GetParam(i)}");
        }
        
        call.Item2.GetParam(0).Should().Be(5);
    }
}
