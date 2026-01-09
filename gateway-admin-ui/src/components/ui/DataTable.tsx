import { Table, Empty } from 'antd';
import type { TableProps } from 'antd';

export interface DataTableProps<T> extends Omit<TableProps<T>, 'locale'> {
  /** Boş durum mesajı */
  emptyText?: string;
}

/**
 * Türkçe locale'li veri tablosu
 */
export function DataTable<T extends object>({
  emptyText = 'Veri bulunamadı',
  ...props
}: DataTableProps<T>) {
  return (
    <Table<T>
      {...props}
      locale={{
        emptyText: <Empty description={emptyText} />,
        filterConfirm: 'Uygula',
        filterReset: 'Sıfırla',
        filterTitle: 'Filtre',
        selectAll: 'Tümünü seç',
        selectInvert: 'Seçimi ters çevir',
        selectionAll: 'Tüm veri',
        sortTitle: 'Sırala',
        triggerDesc: 'Azalan sırala',
        triggerAsc: 'Artan sırala',
        cancelSort: 'Sıralamayı iptal et',
      }}
      pagination={
        props.pagination === false
          ? false
          : {
              showSizeChanger: true,
              showTotal: (total, range) =>
                `${range[0]}-${range[1]} / ${total} kayıt`,
              pageSizeOptions: ['10', '20', '50', '100'],
              ...props.pagination,
            }
      }
    />
  );
}
