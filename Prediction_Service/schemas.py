from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime


class PricePoint(BaseModel):
    ds: datetime
    y: float


class PredictionRequest(BaseModel):
    scraped_item_id: int
    product_name: Optional[str] = None
    history: List[PricePoint] = Field(..., min_items=5)
    forecast_days: int = Field(default=7, ge=1, le=30)


class ForecastPoint(BaseModel):
    ds: datetime
    yhat: float
    yhat_lower: float
    yhat_upper: float


class PredictionResponse(BaseModel):
    scraped_item_id: int
    product_name: Optional[str] = None
    forecasts: List[ForecastPoint]
    model_info: dict