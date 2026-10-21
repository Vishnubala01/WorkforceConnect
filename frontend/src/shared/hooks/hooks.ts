import { useEffect, useState } from 'react';

interface AsyncState<T> {
  data: T | null;
  error: Error | null;
  loading: boolean;
}

/**
 * Runs a request when the dependencies change and exposes loading / error / data.
 * Previous data stays visible while a reload is in flight, so lists do not flash empty.
 */
export function useAsync<T>(load: (signal: AbortSignal) => Promise<T>, deps: readonly unknown[]) {
  const [state, setState] = useState<AsyncState<T>>({ data: null, error: null, loading: true });
  const [tick, setTick] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setState((s) => ({ ...s, loading: true, error: null }));
    load(controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) setState({ data, error: null, loading: false });
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) return;
        setState((s) => ({ ...s, error: error instanceof Error ? error : new Error('Request failed'), loading: false }));
      });
    return () => controller.abort();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick]);

  return { ...state, reload: () => setTick((t) => t + 1) };
}

export function useDebounced<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const handle = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(handle);
  }, [value, delayMs]);
  return debounced;
}
