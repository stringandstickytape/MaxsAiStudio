using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AiStudio4.McpStandalone.McpServer
{
    /// <summary>
    /// A simple hello world tool for MCP server demonstration
    /// </summary>
    [McpServerToolType]
    public class HelloWorldTool
    {
        /// <summary>
        /// Says hello to the specified name
        /// </summary>
        /// <param name="name">The name to greet</param>
        /// <returns>A greeting message</returns>
        [McpServerTool, Description("Says hello to the specified name")]
        public async Task<string> SayHello(
            [Description("The name to greet")] string name = "World")
        {
            await Task.Delay(100); // Simulate some async work
            return $"Hello, {name}! This is from the MCP Standalone server with OAuth authentication.";
        }

        /// <summary>
        /// Gets the current time
        /// </summary>
        /// <returns>The current time as a string</returns>
        [McpServerTool, Description("Gets the current server time")]
        public async Task<string> GetCurrentTime()
        {
            await Task.CompletedTask;
            return $"Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// Performs a simple calculation
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <param name="operation">Operation to perform (add, subtract, multiply, divide)</param>
        /// <returns>The result of the calculation</returns>
        [McpServerTool, Description("Performs a simple calculation")]
        public async Task<string> Calculate(
            [Description("First number")] double a,
            [Description("Second number")] double b,
            [Description("Operation to perform (add, subtract, multiply, divide)")] string operation = "add")
        {
            await Task.CompletedTask;
            
            double result = operation.ToLower() switch
            {
                "add" => a + b,
                "subtract" => a - b,
                "multiply" => a * b,
                "divide" => b != 0 ? a / b : double.NaN,
                _ => double.NaN
            };

            if (double.IsNaN(result))
            {
                return $"Error: Invalid operation '{operation}' or division by zero";
            }

            return $"Result: {a} {operation} {b} = {result}";
        }
    }
}