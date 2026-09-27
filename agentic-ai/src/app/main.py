import os
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import uvicorn

from app.config import settings
from app.api.quality_routes import router as quality_router

app = FastAPI(
    title="AgriConnect Agentic AI Service",
    description="Agentic AI subsystem using LangChain and LangGraph for AgriConnect",
    version="0.1.0"
)

# CORS configuration
allowed_origins = [o.strip() for o in settings.allowed_origins.split(",") if o.strip()]
app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins if allowed_origins else ["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Mount Routers
app.include_router(quality_router)


@app.get("/")
def read_root():
    return {
        "service": "AgriConnect Agentic AI",
        "status": "online",
        "provider": settings.llm_provider,
        "default_model": settings.default_model,
        "gemini_ready": bool(settings.gemini_api_key)
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
