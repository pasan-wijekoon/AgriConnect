import React from 'react';
import { Search, Filter, RotateCcw } from 'lucide-react';
import { Button } from './Button';

export interface SearchFilterBarProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  selectedGrade?: string;
  onGradeChange?: (grade: string) => void;
  placeholder?: string;
  onReset?: () => void;
  extraFilters?: React.ReactNode;
}

export const SearchFilterBar: React.FC<SearchFilterBarProps> = ({
  searchTerm,
  onSearchChange,
  selectedGrade,
  onGradeChange,
  placeholder = 'Search by crop, farmer, or keywords...',
  onReset,
  extraFilters
}) => {
  return (
    <div
      style={{
        display: 'flex',
        flexWrap: 'wrap',
        alignItems: 'center',
        gap: '12px',
        backgroundColor: '#FFFFFF',
        padding: '12px 16px',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--border-color)',
        marginBottom: '20px'
      }}
    >
      <div style={{ position: 'relative', flex: '1 1 240px', minWidth: '200px' }}>
        <Search
          size={18}
          color="var(--text-muted)"
          style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)' }}
        />
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder={placeholder}
          style={{
            width: '100%',
            padding: '8px 12px 8px 36px',
            fontSize: '14px',
            borderRadius: 'var(--radius-sm)',
            border: '1px solid var(--border-color)',
            outline: 'none',
            boxSizing: 'border-box'
          }}
        />
      </div>

      {onGradeChange && (
        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
          <Filter size={16} color="var(--text-secondary)" />
          <select
            value={selectedGrade || ''}
            onChange={(e) => onGradeChange(e.target.value)}
            style={{
              padding: '8px 12px',
              fontSize: '14px',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--border-color)',
              backgroundColor: '#FFFFFF',
              color: 'var(--text-primary)',
              cursor: 'pointer',
              outline: 'none'
            }}
          >
            <option value="">All Quality Grades</option>
            <option value="Grade A">Grade A (Premium)</option>
            <option value="Grade B">Grade B (Standard)</option>
            <option value="Grade C">Grade C (Processing)</option>
            <option value="Rejected">Rejected</option>
          </select>
        </div>
      )}

      {extraFilters}

      {onReset && (
        <Button
          variant="ghost"
          size="sm"
          onClick={onReset}
          icon={<RotateCcw size={14} />}
          style={{ color: 'var(--text-secondary)' }}
        >
          Reset
        </Button>
      )}
    </div>
  );
};
