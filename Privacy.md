Privacy Policy for AiStudio4

Last Updated: [Date]

This PrivacyPolicy describes how AiStudio4 ("the Application," "we," "us," or "our") handles information when you use our Google Drive integration feature.

1. Information We Access
   When you use the "Import from Google AI Studio via Google Drive" feature (and future Google Drive related features), the Application will request your permission to access your Google Drive. The current version requests access (`https://www.googleapis.com/auth/drivehttps://www.googleapis.com/auth/drive`) to list, read and edit files, which it will do within the folder named "Google AI Studio". You will always be shown the specific permissions requested by Google before you grant access.

2. How We Use Your Information
   - File Listing: To list files from your "Google AI Studio" folder for display within the Application.
   - File Content (Future): To import data from files you select into the Application.
   - File Upload (Future): To save data from the Application to files you specify in your Google Drive.
   - OAuth Tokens: Google provides the Application with OAuth 2.0 tokens (access and refresh tokens) upon your authorization. The refresh token is stored securely on your local computer to allow the Application to maintain access to your Google Drive without requiring you to log in repeatedly.
   Your Google Drive file content and metadata are processed locally on your computer by the Application. We do not transmit your file content or metadata to any servers other than Google's servers as part of the authorized API calls.

3. Information Storage
   - OAuth Refresh Tokens: These are stored locally on your computer in a standard application data folder (e.g., `%APPDATA%\AiStudio4\GoogleDriveToken` on Windows). This storage is managed by Google's official API client libraries.
   - Application Data: AiStudio4 stores its own data (like conversation history, settings) locally on your computer. It does not store copies of your Google Drive files.

4. Information Sharing
   AiStudio4 does not share your Google Drive information or OAuth tokens with any third parties. All interactions with Google Drive data are between the Application running on your computer and Google's APIs, as authorized by you.

5. Your Control
   - You can revoke AiStudio4's access to your Google Drive at any time via your Google Account security settings: [https://myaccount.google.com/permissions](https://myaccount.google.com/permissions)
   - You can also manually delete the locally stored OAuth tokens by removing the token storage folder mentioned in section 3.

6. Security
   We use Google's official client libraries to handle OAuth 2.0 authentication and token storage, which are designed with security in mind for local applications. All communication with Google Drive APIs is encrypted using HTTPS.

7. Changes to This Policy
   We may update this Privacy Policy from time to time. We will notify you of any significant changes by posting the new Privacy Policy within the application or on our project website.

8. Contact Us
   If you have any questions about this Privacy Policy, please contact us at [your_support_email@example.com] or open an issue on our GitHub repository at [link_to_your_github_repo_issues].