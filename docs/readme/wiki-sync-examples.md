# Azure DevOps Wiki Sync - Examples and Workflows

This document provides practical examples and workflows for using the Azure DevOps Wiki Sync feature in Max's AI Studio 4.

## Example 1: Team Development Guidelines

### Scenario
Your development team maintains coding standards and AI interaction guidelines in an Azure DevOps wiki page that should automatically update the AI's behavior across all team members' AiStudio4 installations.

### Setup
1. **Wiki Page Structure** (`/Development/AI-Guidelines`):
   ```markdown
   # Development Team AI Guidelines
   
   You are an AI assistant helping with software development for Project Alpha.
   
   ## Coding Standards
   - Use C# 12 features where appropriate
   - Follow Microsoft naming conventions
   - Include XML documentation for public APIs
   - Use dependency injection patterns
   
   ## Project Context
   - This is a .NET 9 web application
   - We use Entity Framework Core for data access
   - Frontend is React with TypeScript
   - Follow clean architecture principles
   
   ## Communication Style
   - Be concise but thorough
   - Provide code examples when helpful
   - Suggest best practices and alternatives
   - Ask clarifying questions when requirements are unclear
   ```

2. **AiStudio4 Configuration**:
   - Organization: `mycompany`
   - Project: `ProjectAlpha`
   - Wiki Identifier: `ProjectAlpha.wiki`
   - Page Path: `/Development/AI-Guidelines`
   - Target System Prompt: "Team Development Guidelines"

### Workflow
1. Team lead updates the wiki page with new guidelines
2. Each team member restarts AiStudio4
3. The system prompt automatically updates with the latest guidelines
4. All AI interactions now follow the updated standards

## Example 2: Project-Specific Instructions

### Scenario
You're working on a specific feature and want the AI to understand the current project context, requirements, and constraints that are documented in your Azure DevOps wiki.

### Setup
1. **Wiki Page Structure** (`/Features/UserAuthentication/AI-Context`):
   ```markdown
   # User Authentication Feature - AI Context
   
   You are helping implement a user authentication system for our web application.
   
   ## Current Requirements
   - Support OAuth 2.0 with Google and Microsoft
   - Implement JWT token-based authentication
   - Include role-based authorization (Admin, User, Guest)
   - Support password reset functionality
   - Implement account lockout after failed attempts
   
   ## Technical Constraints
   - Must use ASP.NET Core Identity
   - Database: SQL Server with Entity Framework Core
   - Frontend: React with TypeScript
   - Must be compatible with existing API structure
   
   ## Security Requirements
   - Passwords must meet complexity requirements
   - Implement CSRF protection
   - Use HTTPS only
   - Log all authentication events
   
   ## Files to Focus On
   - Controllers/AuthController.cs
   - Models/User.cs
   - Services/AuthenticationService.cs
   - ClientApp/src/components/Auth/
   
   When helping with this feature, prioritize security best practices and ensure compatibility with our existing codebase.
   ```

2. **AiStudio4 Configuration**:
   - Page Path: `/Features/UserAuthentication/AI-Context`
   - Target System Prompt: "User Auth Feature Context"

### Workflow
1. Feature requirements are documented in the wiki
2. AI automatically receives context about the specific feature
3. All development assistance is tailored to the current requirements
4. As requirements change, the wiki is updated and AI context refreshes

## Example 3: Code Review Guidelines

### Scenario
Your team wants consistent AI assistance during code reviews, with specific criteria and standards that should be applied uniformly.

### Setup
1. **Wiki Page Structure** (`/Process/Code-Review-AI-Assistant`):
   ```markdown
   # Code Review AI Assistant Guidelines
   
   You are assisting with code reviews for our development team.
   
   ## Review Criteria
   ### Security
   - Check for SQL injection vulnerabilities
   - Verify input validation
   - Ensure proper authentication/authorization
   - Look for hardcoded secrets or credentials
   
   ### Performance
   - Identify potential N+1 query problems
   - Check for inefficient loops or algorithms
   - Verify proper async/await usage
   - Look for memory leaks or resource disposal issues
   
   ### Code Quality
   - Ensure proper error handling
   - Check for code duplication
   - Verify naming conventions
   - Assess code readability and maintainability
   
   ### Testing
   - Verify unit test coverage
   - Check for integration test scenarios
   - Ensure edge cases are covered
   
   ## Review Process
   1. Analyze the code changes systematically
   2. Provide specific, actionable feedback
   3. Suggest improvements with code examples
   4. Highlight both positive aspects and areas for improvement
   5. Consider the broader impact on the codebase
   
   ## Communication Style
   - Be constructive and supportive
   - Explain the reasoning behind suggestions
   - Provide alternative approaches when applicable
   - Focus on learning opportunities
   ```

### Workflow
1. Developer requests AI assistance for code review
2. AI applies consistent review criteria from the wiki
3. Review feedback follows team standards
4. Guidelines can be updated centrally as team practices evolve

## Example 4: API Documentation Standards

### Scenario
Your team maintains API documentation standards that should guide AI assistance when working with API development and documentation.

### Setup
1. **Wiki Page Structure** (`/Standards/API-Documentation`):
   ```markdown
   # API Documentation Assistant
   
   You are helping create and maintain API documentation for our REST APIs.
   
   ## Documentation Standards
   ### Endpoint Documentation
   - Include clear description of purpose
   - Document all parameters (path, query, body)
   - Specify request/response formats
   - Provide example requests and responses
   - Document possible error codes and messages
   
   ### OpenAPI/Swagger Requirements
   - Use OpenAPI 3.0 specification
   - Include comprehensive schemas
   - Add meaningful descriptions for all properties
   - Use appropriate data types and formats
   - Include validation rules and constraints
   
   ## Example Format
   ```yaml
   /api/users/{id}:
     get:
       summary: Get user by ID
       description: Retrieves a specific user by their unique identifier
       parameters:
         - name: id
           in: path
           required: true
           schema:
             type: integer
             format: int64
           description: Unique identifier for the user
       responses:
         '200':
           description: User found successfully
           content:
             application/json:
               schema:
                 $ref: '#/components/schemas/User'
         '404':
           description: User not found
   ```
   
   ## Quality Checklist
   - [ ] All endpoints documented
   - [ ] Request/response examples provided
   - [ ] Error scenarios covered
   - [ ] Authentication requirements specified
   - [ ] Rate limiting information included
   ```

### Workflow
1. Developer works on API endpoints
2. AI provides documentation assistance following team standards
3. Documentation is consistent across all APIs
4. Standards evolve and automatically update AI behavior

## Best Practices

### Wiki Page Organization
- Use clear, hierarchical page paths
- Keep related content together
- Use consistent naming conventions
- Include version information when relevant

### Content Structure
- Start with a clear purpose statement
- Use markdown formatting for readability
- Include specific examples and code snippets
- Organize content logically with headers

### Maintenance
- Regularly review and update wiki content
- Use Azure DevOps wiki version control features
- Coordinate updates with team members
- Test AI behavior after significant changes

### Security Considerations
- Don't include sensitive information in wiki pages
- Use appropriate Azure DevOps permissions
- Regularly audit PAT permissions
- Monitor sync logs for any issues

## Troubleshooting Common Scenarios

### Wiki Content Not Updating
1. Check that the wiki page was actually modified
2. Verify the page path is correct
3. Ensure the Azure DevOps PAT has proper permissions
4. Restart AiStudio4 to trigger a new sync

### System Prompt Conflicts
1. Use unique, descriptive names for system prompts
2. Consider using different prompts for different contexts
3. Regularly clean up unused system prompts
4. Document which prompts are managed by wiki sync

### Team Coordination
1. Establish clear ownership of wiki pages
2. Use Azure DevOps notifications for wiki changes
3. Communicate major changes to the team
4. Consider using wiki page comments for coordination