#!/usr/bin/env python3
"""Run a command with SQL configuration without printing or committing credentials."""
import os
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parent.parent
runtime_env = os.environ.copy()
connection = runtime_env.get('ConnectionStrings__DefaultConnection')
if not connection:
    password = runtime_env.get('MSSQL_SA_PASSWORD')
    if not password:
        local_file = root / '.env'
        if local_file.exists():
            for line in local_file.read_text().splitlines():
                if line.startswith('MSSQL_SA_PASSWORD='):
                    password = line.split('=', 1)[1].strip()
                    if len(password) > 1 and password[0] == password[-1] and password[0] in "\"'":
                        password = password[1:-1]
                    break
    if not password:
        sys.exit('Set ConnectionStrings__DefaultConnection securely, or configure local SQL in ignored .env.')
    escaped_password = password.replace('"', '""')
    connection = ('Server=127.0.0.1,1433;Database=ProjectManagement;User Id=sa;'
                  f'Password="{escaped_password}";Encrypt=True;TrustServerCertificate=True')
runtime_env['ConnectionStrings__DefaultConnection'] = connection
runtime_env.setdefault('PMS_TEST_CONNECTION', connection)
if len(sys.argv) < 2:
    sys.exit('Usage: python scripts/with-database.py <command> [args...]')
sys.exit(subprocess.run(sys.argv[1:], cwd=root, env=runtime_env).returncode)
