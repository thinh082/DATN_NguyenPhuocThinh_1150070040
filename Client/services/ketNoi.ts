import { useEffect, useState } from 'react';
import apiClient from './api';

const CHU_KY_KIEM_TRA_MS = 5000;

export async function kiemTraKetNoiMayChu(): Promise<boolean> {
  try {
    await apiClient.get('/api/ping', { timeout: 3000 });
    return true;
  } catch (error: any) {
    return typeof error?.statusCode === 'number' && error.statusCode > 0;
  }
}

export function useKetNoiMayChu(): boolean | null {
  const [trucTuyen, setTrucTuyen] = useState<boolean | null>(null);
  useEffect(() => {
    let active = true;
    const check = async () => {
      const ok = await kiemTraKetNoiMayChu();
      if (active) setTrucTuyen(ok);
    };
    void check();
    const timer = setInterval(check, CHU_KY_KIEM_TRA_MS);
    return () => { active = false; clearInterval(timer); };
  }, []);
  return trucTuyen;
}
