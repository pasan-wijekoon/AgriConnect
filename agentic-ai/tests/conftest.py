"""Keeps the suite hermetic: no real LLM, no real backend, whatever is in a developer's .env.

src/app/main.py calls load_dotenv() at import time, which copies a local .env (real
LLM_PROVIDER / API key / TOOLS_MODE) into os.environ. Without this file the suite then
makes real LLM calls (slow, costs quota) and the tests that assert the mock-mode
behaviour fail only on machines that have a .env. This module runs before any test module
is imported, so it can pin the environment first.
"""
import os

import dotenv

# main.py's load_dotenv() must not inject the developer's .env into the test process.
dotenv.load_dotenv = lambda *args, **kwargs: False

# Environment variables outrank the .env file for pydantic-settings, so this also
# neutralises the .env that Settings() itself reads when no _env_file override is given.
os.environ["LLM_PROVIDER"] = "mock"
os.environ["GEMINI_API_KEY"] = ""
os.environ["OPENAI_API_KEY"] = ""
