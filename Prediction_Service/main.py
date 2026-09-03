from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import pandas as pd
from prophet_model import train_and_forecast
from schemas import PredictionRequest, PredictionResponse, ForecastPoint
from cache import get_cache, set_cache
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="PC Parts Price Prediction", version="1.0.0")

# app.add_middleware(
#     CORSMiddleware,
#     allow_origins=["*"],
#     allow_methods=["*"],
#     allow_headers=["*"],
# )


@app.get("/health")
async def health():
    return {"status": "ok", "service": "prophet-predictor"}


@app.post("/predict", response_model=PredictionResponse)
async def predict(request: PredictionRequest):
    cache_key = f"predict:{request.scraped_item_id}:{request.forecast_days}"
    cached = get_cache(cache_key)
    if cached:
        logger.info(f"Cache hit for scraped_item_id={request.scraped_item_id}")
        return cached

    logger.info(
        f"Training Prophet for scraped_item_id={request.scraped_item_id} "
        f"with {len(request.history)} data points"
    )

    try:
        df = pd.DataFrame([
            {"ds": p.ds, "y": float(p.y)} for p in request.history
        ])

        forecast_df, model_info = train_and_forecast(
            df, request.forecast_days
        )

        forecast_points = [
            ForecastPoint(
                ds=row.ds.to_pydatetime(),
                yhat=round(float(row.yhat), 2),
                yhat_lower=round(float(row.yhat_lower), 2),
                yhat_upper=round(float(row.yhat_upper), 2),
            )
            for _, row in forecast_df.iterrows()
        ]

        response = PredictionResponse(
            scraped_item_id=request.scraped_item_id,
            product_name=request.product_name,
            forecasts=forecast_points,
            model_info=model_info,
        )

        set_cache(cache_key, response)
        logger.info(f"✅ Prediction cached for {request.scraped_item_id}")
        return response

    except ValueError as ve:
        raise HTTPException(status_code=400, detail=str(ve))
    except Exception as e:
        logger.error(f"❌ Prediction failed: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Prediction service error")


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)