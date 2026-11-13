import axios from 'axios';
import type { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { ApiResponse } from './types/api';

// An explicit API URL enables live mode. The public sample runs entirely in the browser.
const baseURL = import.meta.env.VITE_API_BASE_URL?.trim();
export const isDemoMode = !baseURL;

// التوكن في الذاكرة فقط، مش localStorage (تقليلًا لخطر XSS). الأثر الجانبي:
// تحديث الصفحة (F5) يسجّل خروج المستخدم لحد ما يُضاف httpOnly Cookie من الباك.
let inMemoryToken: string | null = null;
export function setAuthToken(token: string | null) {
  inMemoryToken = token;
}

let onUnauthorized: (() => void) | null = null;
export function registerUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler;
}

export const apiClient = axios.create({
  baseURL,
  adapter: isDemoMode ? async (config) => (await import('@/demo/demo-adapter')).demoAdapter(config) : undefined,
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (inMemoryToken) {
    config.headers.Authorization = `Bearer ${inMemoryToken}`;
  }
  return config;
});

/** خطأ مُطبَّع بيحمل الرسالة العربية الجاهزة من الباك إند + كود الخطأ لو موجود */
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly errorCode: string | null = null
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiResponse<unknown>>) => {
    if (error.response?.status === 401) {
      onUnauthorized?.();
    }
    const envelope = error.response?.data;
    return Promise.reject(
      new ApiError(envelope?.message ?? 'تعذّر الاتصال بالخادم، حاول مرة أخرى', envelope?.errorCode ?? null)
    );
  }
);

/** يستخرج data من غلاف ApiResponse<T> بعد نجاح الطلب (القسم 4.3) */
export async function unwrap<T>(promise: Promise<{ data: ApiResponse<T> }>): Promise<T> {
  const { data: envelope } = await promise;
  if (!envelope.success || envelope.data === null) {
    throw new ApiError(envelope.message ?? 'حدث خطأ غير متوقع', envelope.errorCode);
  }
  return envelope.data;
}

/** true لو الخطأ ده تحديدًا تعارض RowVersion (طلبين حصلوا في نفس اللحظة) */
export function isConcurrencyConflict(err: unknown): boolean {
  return err instanceof ApiError && err.errorCode === 'CONCURRENCY_CONFLICT';
}
