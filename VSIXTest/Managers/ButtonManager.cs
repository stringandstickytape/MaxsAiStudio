using System;
using System.Collections.Generic;
using System.Text;
using SharedClasses;

namespace VSIXTest
{
    public class ButtonManager
    {
        /// <summary>
        /// Represents a collection of predefined message prompts for various code-related tasks.
        /// </summary>
        /// <remarks>
        /// This array contains <see cref="MessagePrompt"/> objects categorized by different aspects of code analysis, 
        /// refactoring, enhancement, and documentation. Each prompt is associated with a specific button label and message type.
        /// </remarks>
        public static readonly MessagePrompt[] MessagePrompts = new MessagePrompt[]
        {


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