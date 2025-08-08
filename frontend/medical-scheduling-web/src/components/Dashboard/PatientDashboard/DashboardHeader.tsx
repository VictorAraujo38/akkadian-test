import React from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { useRouter } from 'next/router'
import { PlusIcon } from '@heroicons/react/24/outline'
import styles from '../PatientDashboard.module.css'

export const DashboardHeader: React.FC = () => {
  const { user } = useAuth()
  const router = useRouter()

  return (
    <div className={styles.header}>
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Olá, {user?.name}!</h1>
          <p className="text-white/80 mt-1">Gerencie seus agendamentos médicos</p>
        </div>
        <button
          onClick={() => router.push('/paciente/novo-agendamento')}
          className="bg-white text-purple-600 px-6 py-3 rounded-lg font-semibold hover:bg-gray-100 transition-colors flex items-center gap-2"
        >
          <PlusIcon className="h-5 w-5" />
          Novo Agendamento
        </button>
      </div>
    </div>
  )
}