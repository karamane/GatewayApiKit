import React from 'react';
import { Switch, Space, Tag, Tooltip } from 'antd';

export interface NodeToggleProps {
  /** Aktif durumu */
  checked: boolean;
  /** Değişiklik callback'i */
  onChange: (checked: boolean) => void;
  /** Yükleniyor durumu */
  loading?: boolean;
  /** Devre dışı mı? */
  disabled?: boolean;
  /** Devre dışı nedeni (tooltip) */
  disabledReason?: string;
  /** Label göster */
  showLabel?: boolean;
}

/**
 * Node etkinleştirme/devre dışı bırakma toggle'ı
 */
export const NodeToggle: React.FC<NodeToggleProps> = ({
  checked,
  onChange,
  loading = false,
  disabled = false,
  disabledReason,
  showLabel = true,
}) => {
  const toggle = (
    <Space>
      <Switch
        checked={checked}
        onChange={onChange}
        loading={loading}
        disabled={disabled}
        size="small"
      />
      {showLabel && (
        <Tag color={checked ? 'success' : 'default'}>
          {checked ? 'AKTİF' : 'PASİF'}
        </Tag>
      )}
    </Space>
  );

  if (disabled && disabledReason) {
    return (
      <Tooltip title={disabledReason}>
        <span>{toggle}</span>
      </Tooltip>
    );
  }

  return toggle;
};
