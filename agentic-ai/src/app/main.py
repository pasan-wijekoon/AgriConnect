import os
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import uvicorn

from src.app.api.logistics import router as logistics_router

app = FastAPI(
    title="AgriConnect Agentic AI Service",
    description="Agentic AI subsystem using LangChain and LangGraph for AgriConnect",
    version="0.1.0"
)

# CORS configuration
allowed_origins = os.getenv("ALLOWED_ORIGINS", "*").split(",")
app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


app.include_router(logistics_router)


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
