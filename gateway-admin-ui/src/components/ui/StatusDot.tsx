import React from 'react';
import { Tooltip } from 'antd';

export interface StatusDotProps {
  /** Durum: true = yeşil, false = kırmızı */
  status: boolean;
  /** Tooltip metni */
  hint?: string;
  /** Animasyonlu olsun mu? */
  pulse?: boolean;
  /** Boyut (px) */
  size?: number;
}

/**
 * Durum göstergesi - yeşil (aktif) veya kırmızı (pasif)
 */
export const StatusDot: React.FC<StatusDotProps> = ({
  status,
  hint,
  pulse = false,
  size = 10,
}) => {
  const style: React.CSSProperties = {
    width: size,
    height: size,
    borderRadius: '50%',
    backgroundColor: status ? '#059669' : '#dc2626',
    display: 'inline-block',
    boxShadow: status ? '0 0 6px rgba(5, 150, 105, 0.4)' : '0 0 6px rgba(220, 38, 38, 0.4)',
    animation: pulse ? 'pulse 2s infinite' : undefined,
  };

  const dot = <span style={style} />;

  if (hint) {
    return (
      <Tooltip title={hint} placement="top">
        {dot}
      </Tooltip>
    );
  }

  return dot;
};
