import { useEffect, useState } from "react";
import { api } from "../api/client";

const CACHE_TTL = 5 * 60 * 1000;

interface CachedRate {
  rate: number;
  disclaimer: string;
  ts: number;
}

let cached: CachedRate | null = null;
let inflight: Promise<CachedRate | null> | null = null;

function fresh(): boolean {
  return !!cached && Date.now() - cached.ts < CACHE_TTL;
}

function loadRate(): Promise<CachedRate | null> {
  if (fresh()) return Promise.resolve(cached);
  if (!inflight) {
    inflight = api
      .brlRate()
      .then((r) => {
        cached = { rate: r.brlRate, disclaimer: r.disclaimer, ts: Date.now() };
        return cached;
      })
      .catch(() => {
        inflight = null;
        return null;
      });
  }
  return inflight;
}

export interface BrlRateState {
  rate: number | null;
  disclaimer: string | null;
  loading: boolean;
}

export function useBrlRate(): BrlRateState {
  const [state, setState] = useState<BrlRateState>(() =>
    cached
      ? { rate: cached.rate, disclaimer: cached.disclaimer, loading: false }
      : { rate: null, disclaimer: null, loading: true }
  );

  useEffect(() => {
    if (fresh()) {
      setState({ rate: cached!.rate, disclaimer: cached!.disclaimer, loading: false });
      return;
    }
    let active = true;
    loadRate().then((res) => {
      if (!active) return;
      if (res) setState({ rate: res.rate, disclaimer: res.disclaimer, loading: false });
      else setState({ rate: null, disclaimer: null, loading: false });
    });
    return () => {
      active = false;
    };
  }, []);

  return state;
}
