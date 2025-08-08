import React from 'react'
import styles from '../PatientDashboard.module.css'

interface StatsCardsProps {
  stats: {
    total: number
    upcoming: number
    completed: number
  }
}

export const StatsCards: React.FC<StatsCardsProps> = ({ stats }) => {
  return (
    <div className={styles.statsGrid}>
      <div className={styles.statCard}>
        <div className="flex items-center justify-between">
          <div>
            <p className="text-gray-500 text-sm">Total de Consultas</p>
            <p className="text-3xl font-bold text-gray-800">{stats.total}</p>
          </div>
          <div className="bg-blue-100 p-3 rounded-full">
            <svg className="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
          </div>
        </div>
      </div>

      <div className={styles.statCard}>
        <div className="flex items-center justify-between">
          <div>
            <p className="text-gray-500 text-sm">Próximas Consultas</p>
            <p className="text-3xl font-bold text-green-600">{stats.upcoming}</p>
          </div>
          <div className="bg-green-100 p-3 rounded-full">
            <svg className="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
        </div>
      </div>

      <div className={styles.statCard}>
        <div className="flex items-center justify-between">
          <div>
            <p className="text-gray-500 text-sm">Consultas Realizadas</p>
            <p className="text-3xl font-bold text-purple-600">{stats.completed}</p>
          </div>
          <div className="bg-purple-100 p-3 rounded-full">
            <svg className="w-8 h-8 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
        </div>
      </div>
    </div>
  )
}