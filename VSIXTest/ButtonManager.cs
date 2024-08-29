using System;
using System.Collections.Generic;
using System.Text;
using SharedClasses;

namespace VSIXTest
{
    public class ButtonManager
    {
        public static readonly MessagePrompt[] MessagePrompts = new[]
        {
            // Code Analysis and Explanation
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Explain Code", MessageType = "explainCode", Prompt = "Provide a detailed explanation of what this code does:" },
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Identify Potential Bugs", MessageType = "identifyBugs", Prompt = "Analyze this code for potential bugs or edge cases that might cause issues:" },

            // Code Improvement and Refactoring
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Extract Method", MessageType = "extractMethod", Prompt = "Perform an extract method on this:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Extract Static Method", MessageType = "extractStaticMethod", Prompt = "Perform an extract static method on this:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "DRY This", MessageType = "dryThis", Prompt = "Suggest some clever ways, with examples, to DRY this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "General Refactor", MessageType = "generalRefactor", Prompt = "Suggest some clever ways, with examples, to generally refactor this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Improve Performance", MessageType = "improvePerformance", Prompt = "Analyse and, if possible, suggest some clever ways with examples, to improve the performance of this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Simplify Logic", MessageType = "simplifyLogic", Prompt = "Analyze and suggest ways to simplify the logic in this code without changing its functionality:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "Convert to LINQ", MessageType = "convertToLinq", Prompt = "Convert this code to use LINQ expressions where appropriate:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "Extract Best Class", MessageType = "extractBestClass", Prompt = "Analyze this code and identify the single best class that could be extracted to improve general Object-Oriented Programming (OOP) principles. Describe the proposed class, its properties, methods, and how it would enhance the overall design:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "String Interpolation", MessageType = "stringInterpolation", Prompt = "Rewrite this to use string interpolation:" },

            // Code Enhancement
            new MessagePrompt { Category = "Enhancement", ButtonLabel = "Add Error Handling", MessageType = "addErrorHandling", Prompt = "Suggest appropriate error handling mechanisms for this code:" },
            new MessagePrompt { Category = "Enhancement", ButtonLabel = "Add Logging", MessageType = "addLogging", Prompt = "Suggest appropriate logging statements to add to this code for better debugging and monitoring:" },

            // Naming and Documentation
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Suggest Name", MessageType = "suggestName", Prompt = "Suggest a concise and descriptive name for this code element:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Commit Message", MessageType = "commitMsg", Prompt = "Give me a short, high-quality, bulleted, tersely-phrased summary for this diff.  Break the changes down by project and category.  Demarcate the summary as a single code block. Do not mention unused categories or insignficiant changes." },

            // Code Generation and Extension
            new MessagePrompt { Category = "Generation", ButtonLabel = "Autocomplete at //! marker", MessageType = "autocompleteThis", Prompt = "Autocomplete this code where you see the marker //! . Give only the inserted text and no other output, demarcated with three ticks before and after." },
            new MessagePrompt { Category = "Generation", ButtonLabel = "Extend Series", MessageType = "addToSeries", Prompt = "Extend the series you see in this code:" },
            new MessagePrompt { Category = "Generation", ButtonLabel = "Create Unit Tests", MessageType = "createUnitTests", Prompt = "Generate unit tests for this code:" },

            // Code Readability
            new MessagePrompt { Category = "Readability", ButtonLabel = "Add Comments", MessageType = "addComments", Prompt = "Add appropriate comments to this code to improve its readability:" },
            new MessagePrompt { Category = "Readability", ButtonLabel = "Remove Comments", MessageType = "removeComments", Prompt = "Remove all comments from this code:" },

            // User Documentation
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Generate README", MessageType = "generateReadme", Prompt = "Generate a comprehensive README.md file for this project based on the code provided:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Create User Guide", MessageType = "createUserGuide", Prompt = "Create a user guide explaining how to use the functionality implemented in this code:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "API Documentation", MessageType = "generateApiDocs", Prompt = "Generate API documentation for the public methods and classes in this code:" },
        };

        public string GenerateButtonScript()
        {
            var scriptBuilder = new StringBuilder();
            scriptBuilder.Append(@"
    // Get the button container
    var buttonContainer = document.getElementById('ButtonContainer');

    // Clear existing buttons
    buttonContainer.innerHTML = '';

    // Function to create a button
    function createButton(label, messageType) {
        var button = document.createElement('button');
        button.textContent = label;
        button.onclick = function() {
            performAction(messageType);
        };
        return button;
    }

    // Group prompts by category
    var groupedPrompts = {};
    ");

            foreach (var prompt in MessagePrompts)
            {
                scriptBuilder.Append($@"
    if (!groupedPrompts['{prompt.Category}']) {{
        groupedPrompts['{prompt.Category}'] = [];
    }}
    groupedPrompts['{prompt.Category}'].push({{ label: '{prompt.ButtonLabel}', messageType: '{prompt.MessageType}' }});
    ");
            }

            scriptBuilder.Append(@"
    // Create category boxes and add buttons
    for (var category in groupedPrompts) {
        var categoryBox = document.createElement('div');
        categoryBox.className = 'category-box';
        var categoryTitle = document.createElement('div');
        categoryTitle.className = 'category-title';
        categoryTitle.textContent = category;
        categoryBox.appendChild(categoryTitle);

        groupedPrompts[category].forEach(function(prompt) {
            var button = createButton(prompt.label, prompt.messageType);
            categoryBox.appendChild(button);
        });

        buttonContainer.appendChild(categoryBox);
    }

    // Add 'New' button
    var newButton = createButton('New', 'newChat');
    buttonContainer.appendChild(newButton);
    ");

            return scriptBuilder.ToString();
        }

        public string CreateCategoryBox(string category, List<MessagePrompt> prompts)
        {
            var scriptBuilder = new StringBuilder();
            scriptBuilder.Append($@"
    var categoryBox = document.createElement('div');
    categoryBox.className = 'category-box';
    var categoryTitle = document.createElement('div');
    categoryTitle.className = 'category-title';
    categoryTitle.textContent = '{category}';
    categoryBox.appendChild(categoryTitle);

    ");

            foreach (var prompt in prompts)
            {
                scriptBuilder.Append($@"
    var button = createButton('{prompt.ButtonLabel}', '{prompt.MessageType}');
    categoryBox.appendChild(button);
    ");
            }

            scriptBuilder.Append(@"
    buttonContainer.appendChild(categoryBox);
    ");

            return scriptBuilder.ToString();
        }

        public string CreateButton(string label, string messageType)
        {
            return $@"
    var button = createButton('{label}', '{messageType}');
    buttonContainer.appendChild(button);
    ";
        }

        public string AddNewButton()
        {
            return @"
    var newButton = createButton('New', 'newChat');
    buttonContainer.appendChild(newButton);
    ";
        }
    }
}