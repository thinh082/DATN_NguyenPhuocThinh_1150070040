import axios, { AxiosResponse } from 'axios';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

let accessToken: string | null = null;
export async function setAccessToken(token: string | null): Promise<void> {
  accessToken = token;
  if (Platform.OS !== 'web') {
    if (token) await SecureStore.setItemAsync('smartbus_access_token', token);
    else await SecureStore.deleteItemAsync('smartbus_access_token');
  }
}

// Cấu hình URL Backend qua biến môi trường EXPO_PUBLIC_API_BASE_URL
// Fallback theo nền tảng: Android Emulator dùng 10.0.2.2:5000, Web/Desktop dùng localhost:5000
const envUrl = process.env.EXPO_PUBLIC_API_BASE_URL?.trim();
export const BASE_API_URL =
  envUrl || (Platform.OS === 'android' ? 'http://10.0.2.2:5000' : 'http://localhost:5000');

export interface PhanHoiApi<T = any> {
  data: T;
  statusCode: number;
  message: string;
}

export const apiClient = axios.create({
  baseURL: BASE_API_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
    // Cau hinh bat buoc cua ngrok free de bo qua man hinh canh bao trung gian
    'ngrok-skip-browser-warning': 'true',
  },
});

apiClient.interceptors.request.use((config) => {
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`;
  return config;
});

apiClient.interceptors.response.use(
  (response: AxiosResponse<PhanHoiApi>) => {
    return response;
  },
  (error) => {
    if (error.response?.status === 401) void setAccessToken(null);
    const loiPhanHoi: PhanHoiApi = {
      data: null,
      statusCode: error.response?.status || 0,
      message: error.response?.data?.message || error.message || 'Lỗi kết nối đến máy chủ',
    };
    return Promise.reject(loiPhanHoi);
  }
);

export default apiClient;
