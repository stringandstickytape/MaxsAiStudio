# NuGet Package Dependencies for MCP Standalone

## Current Main App Dependencies

### Core MCP/OAuth Packages
```xml
<PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.3" />
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.3" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.7" />
<ProjectReference Include="..\ModelContextProtocol.TestOAuthServer\ModelContextProtocol.TestOAuthServer.csproj" />
```

### Web/API Packages
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.4" />
<FrameworkReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.AspNetCore.SpaProxy" Version="9.0.4" />
<PackageReference Include="Microsoft.AspNetCore.SpaServices.Extensions" Version="9.0.4" />
```

### Google Services (for tools)
```xml
<PackageReference Include="Google.Apis.Auth" Version="1.69.0" />
<PackageReference Include="Google.Apis.Core" Version="1.69.0" />
<PackageReference Include="Google.Apis.Drive.v3" Version="1.69.0.3783" />
```

### Other Tool Dependencies
```xml
<PackageReference Include="Ignore" Version="0.2.1" />  <!-- For .gitignore parsing -->
<PackageReference Include="Microsoft.CodeAnalysis.Workspaces.MSBuild" Version="4.13.0" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.2" />
<PackageReference Include="OpenAI" Version="2.2.0-beta.4" />  <!-- Not needed in standalone -->
```

### Configuration/UI
```xml
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.4" />
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3240.44" />
```

### Shared Project
```xml
<ProjectReference Include="..\SharedClasses\SharedClasses.csproj" />
```

## Required for Core Library (AiStudio4.Core)

### Essential Packages
```xml
<!-- MCP Protocol -->
<PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.3" />
<PackageReference Include="ModelContextProtocol.Protocol" Version="0.3.0-preview.3" />
<PackageReference Include="ModelContextProtocol.Server" Version="0.3.0-preview.3" />

<!-- JSON Handling -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="System.Text.Json" Version="9.0.0" />

<!-- DI and Logging Abstractions -->
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="9.0.0" />

<!-- For .gitignore support in tools -->
<PackageReference Include="Ignore" Version="0.2.1" />

<!-- Database tools -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.2" />

<!-- Code analysis tools -->
<PackageReference Include="Microsoft.CodeAnalysis.Workspaces.MSBuild" Version="4.13.0" />

<!-- Google APIs for search tools -->
<PackageReference Include="Google.Apis.Core" Version="1.69.0" />

<!-- Shared Classes -->
<ProjectReference Include="..\SharedClasses\SharedClasses.csproj" />
```

## Required for Standalone App (AiStudio4.McpServer)

### Core Framework
```xml
<!-- WPF Application -->
<OutputType>WinExe</OutputType>
<TargetFramework>net9.0-windows</TargetFramework>
<UseWPF>true</UseWPF>

<!-- Core Library -->
<ProjectReference Include="..\AiStudio4.Core\AiStudio4.Core.csproj" />
<ProjectReference Include="..\ModelContextProtocol.TestOAuthServer\ModelContextProtocol.TestOAuthServer.csproj" />
```

### MCP Server Hosting
```xml
<!-- MCP and OAuth -->
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.3" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.7" />
<FrameworkReference Include="Microsoft.AspNetCore.App" />

<!-- API Support -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.4" />
```

### Application Framework
```xml
<!-- Hosting and DI -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.4" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />

<!-- Logging -->
<PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
```

### UI Framework
```xml
<!-- MVVM Support -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />

<!-- Modern UI (choose one) -->
<PackageReference Include="ModernWpfUI" Version="0.9.6" />
<!-- OR -->
<PackageReference Include="MahApps.Metro" Version="2.4.10" />

<!-- Notifications -->
<PackageReference Include="Notifications.Wpf.Core" Version="2.0.1" />
```

### Security
```xml
<!-- For credential encryption -->
<PackageReference Include="System.Security.Cryptography.ProtectedData" Version="9.0.0" />
```

## Package Version Alignment

To ensure compatibility between projects:

### .NET 9.0 Packages
All Microsoft.Extensions.* packages should use version 9.0.x:
- Microsoft.Extensions.Hosting: 9.0.0
- Microsoft.Extensions.Configuration.Json: 9.0.4
- Microsoft.Extensions.DependencyInjection: 9.0.0
- Microsoft.Extensions.Logging: 9.0.0

### ModelContextProtocol Packages
All MCP packages should use the same preview version:
- ModelContextProtocol: 0.3.0-preview.3
- ModelContextProtocol.AspNetCore: 0.3.0-preview.3
- ModelContextProtocol.Protocol: 0.3.0-preview.3
- ModelContextProtocol.Server: 0.3.0-preview.3

### Google APIs
Keep consistent versions:
- Google.Apis.Core: 1.69.0
- Google.Apis.Auth: 1.69.0
- Google.Apis.Drive.v3: 1.69.0.3783

## Packages NOT Needed in Standalone

These packages are only needed in the main app:
```xml
<!-- AI Services -->
<PackageReference Include="OpenAI" Version="2.2.0-beta.4" />

<!-- WebView for embedded browser -->
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3240.44" />

<!-- SPA support -->
<PackageReference Include="Microsoft.AspNetCore.SpaProxy" Version="9.0.4" />
<PackageReference Include="Microsoft.AspNetCore.SpaServices.Extensions" Version="9.0.4" />

<!-- Google Drive integration -->
<PackageReference Include="Google.Apis.Drive.v3" Version="1.69.0.3783" />
```

## Build Order

1. SharedClasses (no changes needed)
2. ModelContextProtocol.TestOAuthServer (no changes needed)
3. AiStudio4.Core (new project)
4. AiStudio4 (updated to use Core)
5. AiStudio4.McpServer (new project)