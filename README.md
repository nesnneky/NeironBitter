# Advanced Antivirus

## Overview

Advanced Antivirus is a lightweight antivirus application that performs comprehensive scans of user directories, identifies suspicious files based on their hash values, and sends detailed reports and alerts via Discord webhooks.

## Features

- Scans directories including Downloads, Desktop, and Program Files.
- Detects suspicious files based on known malware signatures.
- Performs basic behavioral analysis of files.
- Sends detailed scan reports and alerts via Discord webhook.

## Prerequisites

- .NET 6.0 or later
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) package
- [DiscordRPC](https://www.nuget.org/packages/DiscordRPC/) package

## Setup

1. **Clone the repository:**

    ```sh
    git clone https://github.com/your-username/advanced-antivirus.git
    cd advanced-antivirus
    ```

2. **Install dependencies:**

    Make sure you have [NuGet](https://www.nuget.org/) installed, and restore the packages.

    ```sh
    dotnet restore
    ```

3. **Configure the application:**

    On the first run, the application will prompt you to enter a Discord webhook URL. This URL will be used to send alerts and reports.

    Ensure you have appropriate permissions to write to the `C:\antivirus_settings.txt` file or adjust the path in the code.

4. **Build and run the application:**

    ```sh
    dotnet build
    dotnet run
    ```

## Usage

- The application scans specified directories every 5 minutes.
- If more than 10 suspicious files are detected, an alert will be sent to the Discord webhook.
- Scan results are saved in a file `scan_report.txt` and also sent to the Discord webhook.

## Troubleshooting

- **Unauthorized Access Exception**: Ensure the application is running with administrative privileges if it needs access to restricted paths.

## Donate

If you appreciate this project and want to support its development, you can make a donation using the following cryptocurrency wallet addresses:

- **USDT (TON):** `UQBORa-Uk87aHLMd7gB3Z1Eg7cqso-7S4-LMSVCG32LfObaS`
- **USDT (TRC20):** `TRQttfwjbSQ51iR6zntV1XkwGcFtGX4ewK`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact

For any issues or contributions, please reach out via GitHub Issues or email me at [your-email@example.com](mailto:csgotpjing@gmail.com).
