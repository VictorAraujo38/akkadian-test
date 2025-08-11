import React, { useState, useEffect } from 'react'
import { useRouter } from 'next/router'
import { useAuth } from '@/contexts/AuthContext'
import { api } from '@/services/api'
import { format } from 'date-fns'
import { ptBR } from 'date-fns/locale'
import { toast } from 'react-toastify'
import styles from './DoctorDashboard.module.css'
import { DashboardHeader } from './components/DashboardHeader'
import { AppointmentCalendar } from './components/AppointmentCalendar'
import { PatientList } from './components/PatientList'
import { StatsCards } from './components/StatsCards'

export const DoctorDashboard: React.FC = () => {
    const router = useRouter()
    const { user, loading: authLoading } = useAuth() // Renomeado para evitar confusão
    const [appointments, setAppointments] = useState<any[]>([])
    const [selectedDate, setSelectedDate] = useState(new Date())
    const [loading, setLoading] = useState(true)
    const [viewMode, setViewMode] = useState<'day' | 'week' | 'month'>('month')

    // Efeito para validação de role - só executa quando user estiver carregado
    useEffect(() => {
        if (!authLoading && user) {
            if (user.role !== 'Doctor') {
               
                router.push('/')
                return
            }
            
            fetchAppointments()
        } else if (!authLoading && !user) {
            
            router.push('/login')
        }
    }, [authLoading, user?.role, selectedDate, viewMode])

    const fetchAppointments = async () => {
        if (!user?.id) {
            
            return
        }

        setLoading(true)
        try {
            
            const dateStr = format(selectedDate, 'yyyy-MM-dd')
            const response = await api.get(`/medico/agendamentos?data=${dateStr}`)

            setAppointments(response.data || [])

        } catch (error) {
            console.error('Erro ao carregar agendamentos:', error)
            toast.error('Erro ao carregar agendamentos')
            setAppointments([])
        } finally {
            setLoading(false)
        }
    }

    // Loading enquanto autentica
    if (authLoading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
                    <p className="text-gray-600">Carregando...</p>
                </div>
            </div>
        )
    }

    // Se não há usuário, não renderiza nada (vai redirecionar)
    if (!user) {
        return null
    }

    return (
        <div className={styles.container}>
            <DashboardHeader user={user} />
            <StatsCards appointments={appointments} selectedDate={selectedDate} />

            <div className={styles.mainContent}>
                <div className={styles.calendarSection}>
                    <AppointmentCalendar
                        appointments={appointments}
                        selectedDate={selectedDate}
                        onDateChange={setSelectedDate}
                        viewMode={viewMode}
                        onViewModeChange={setViewMode}
                        loading={loading}
                    />
                </div>

                <div className={styles.sidePanel}>
                    <PatientList
                        appointments={appointments}
                        selectedDate={selectedDate}
                        loading={loading}
                    />
                </div>
            </div>
        </div>
    )
}