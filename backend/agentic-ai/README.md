# BuildFlow AI agent service

Scaffold only. The named agents, tools, schemas, validator and workflow modules
follow the complete workflow plan. No AI framework or HTTP server is selected yet.
requirements.txt deliberately contains no dependencies.

ASP.NET Core is the only client of this internal service. React and Flutter call
ASP.NET Core. Agents propose plans; the backend validates them and requires manager
approval before committing resource allocations or procurement.

Copy .env.example to .env when implementing configuration. Never commit secrets.
