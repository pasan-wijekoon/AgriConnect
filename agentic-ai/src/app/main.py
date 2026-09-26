import os
from dotenv import load_dotenv
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import uvicorn
from .api.routes import router as api_router

# Load agentic-ai/.env (LLM_PROVIDER, API keys, INTERNAL_API_SECRET, ALLOWED_ORIGINS, ...).
# No-ops silently if the file doesn't exist, so a bare checkout still runs in mock mode.
load_dotenv()

app = FastAPI(
    title="AgriConnect Agentic AI Service",
    description="Agentic AI subsystem using LangChain and LangGraph for AgriConnect",
    version="0.1.0"
)

# Mount API router under both /api and root
app.include_router(api_router, prefix="/api")
app.include_router(api_router)

# CORS configuration — this service is internal-only (called by the .NET backend,
# never directly by browsers), so it defaults to no cross-origin access at all.
# Set ALLOWED_ORIGINS explicitly if a browser-based tool needs to hit it directly.
allowed_origins = [o.strip() for o in os.getenv("ALLOWED_ORIGINS", "").split(",") if o.strip()]
app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=bool(allowed_origins),
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/")
def read_root():
    return {
        "service": "AgriConnect Agentic AI",
        "status": "online",
        "provider": os.getenv("LLM_PROVIDER", "mock")
    }


@app.get("/health")
def health_check():
    return {"status": "healthy"}


def main():
    port = int(os.getenv("PORT", "8000"))
    host = os.getenv("HOST", "0.0.0.0")
    uvicorn.run("src.app.main:app", host=host, port=port, reload=True)


if __name__ == "__main__":
    main()
