from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import pandas as pd

from ProphetModel import train_and_forecast
from schemas import PredictionRequest, PredictionResponse, ForecastPoint
from cache import get_cache, set_cache
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="PC Parts Price Prediction", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:5173",
        "https://your-production-frontend.com",
    ],
    allow_methods=["*"],
    allow_headers=["*"],
)

MIN_DATA_POINTS = 5


@app.get("/health")
async def health():
    return {"status": "ok", "service": "prophet-predictor"}

@app.post("/predict", response_model=PredictionResponse)
def predict(request: PredictionRequest):

    if len(request.history) < MIN_DATA_POINTS:
        raise HTTPException(
            status_code=400,
            detail=f"Not enough price history to forecast (need at least {MIN_DATA_POINTS} points, got {len(request.history)})."
        )

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
        df['ds'] = pd.to_datetime(df['ds']).dt.tz_localize(None)
        df = df.sort_values("ds").drop_duplicates(subset="ds", keep="last").reset_index(drop=True)

        if len(df) < MIN_DATA_POINTS:
            raise HTTPException(
                status_code=400,
                detail=f"Not enough unique data points after deduplication (need at least {MIN_DATA_POINTS}, got {len(df)})."
            )

        forecast_df, model_info = train_and_forecast(df, request.forecast_days)
        # Convert to dictionaries to avoid Pandas/Pydantic type crashes
        forecast_records = forecast_df.to_dict(orient="records")

        forecast_points = []
        for rec in forecast_records:
            forecast_points.append(
                ForecastPoint(
                    ds=pd.Timestamp(rec["ds"]).to_pydatetime(),
                    yhat=round(float(rec["yhat"]), 2),
                    yhat_lower=round(float(rec["yhat_lower"]), 2),
                    yhat_upper=round(float(rec["yhat_upper"]), 2),
                )
            )

        response = PredictionResponse(
            scraped_item_id=request.scraped_item_id,
            product_name=request.product_name,
            forecasts=forecast_points,
            model_info=model_info,
        )

        set_cache(cache_key, response)
        logger.info(f"Prediction cached for {request.scraped_item_id}")
        return response

    except HTTPException:
        raise
    except ValueError as ve:
        raise HTTPException(status_code=400, detail=str(ve))
    except Exception as e:
        logger.error(f"Prediction failed: {e}", exc_info=True)
        raise HTTPException(
            status_code=500,
            detail=f"Prediction service error: {type(e).__name__}: {e}",
        )


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)