import pandas as pd
import numpy as np
import logging
from prophet import Prophet

logging.getLogger('prophet').setLevel(logging.WARNING)
logging.getLogger('cmdstanpy').setLevel(logging.WARNING)


def train_and_forecast(df: pd.DataFrame, forecast_days: int = 7) -> tuple:

    if len(df) < 5:
        raise ValueError("Need at least 5 data points for forecasting")

    df = df.groupby('ds').agg({'y': 'mean'}).reset_index().sort_values('ds')

    if len(df) > 10:
        mean, std = df['y'].mean(), df['y'].std()
        if std > 0:
            df = df[(df['y'] > mean - 3 * std) & (df['y'] < mean + 3 * std)]

    if len(df) < 5:
        raise ValueError("Not enough data after outlier removal")

    model = Prophet(
        growth='linear',
        changepoint_prior_scale=0.1,  # More flexible trend changes
        seasonality_mode='additive',
        yearly_seasonality=False,  # Not enough data for yearly
        weekly_seasonality='auto',
        daily_seasonality=False,  # We don't have intraday data
        interval_width=0.85,  # 85% confidence
    )

    model.fit(df)

    future = model.make_future_dataframe(periods=forecast_days, freq='D')
    forecast = model.predict(future)

    # Extract only future predictions
    predictions = forecast.tail(forecast_days).copy()

    # set negative prices to not less than 0
    predictions.loc[:, 'yhat'] = predictions['yhat'].clip(lower=0)
    predictions.loc[:, 'yhat_lower'] = predictions['yhat_lower'].clip(lower=0)
    predictions.loc[:, 'yhat_upper'] = predictions['yhat_upper'].clip(lower=0)

    model_info = {
        "data_points": len(df),
        "date_range": f"{df['ds'].min().date()} to {df['ds'].max().date()}",
        "trend_direction": "up" if predictions['yhat'].iloc[-1] > predictions['yhat'].iloc[0] else "down",
        "confidence_interval": "85%",
    }

    return predictions[['ds', 'yhat', 'yhat_lower', 'yhat_upper']], model_info