import time
from typing import Optional

_cache = {}
CACHE_TTL = 3600  # 1 hour


def get_cache(key: str):
    if key in _cache:
        data, expiry = _cache[key]
        if time.time() < expiry:
            return data
        del _cache[key]
    return None


def set_cache(key: str, data):
    _cache[key] = (data, time.time() + CACHE_TTL)


def clear_cache():
    _cache.clear()