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
    const { user } = useAuth()
    const [appointments, setAppointments] = useState([])
    const [selectedDate, setSelectedDate] = useState(new Date())
    const [loading, setLoading] = useState(true)
    const [viewMode, setViewMode] = useState<'day' | 'week' | 'month'>('day')

    useEffect(() => {
        if (user?.role !== 'Doctor') {
            router.push('/')
            return
        }
        fetchAppointments()
    }, [user, router, selectedDate])

    const fetchAppointments = async () => {
        setLoading(true)
        try {
            const dateStr = format(selectedDate, 'yyyy-MM-dd')
            const response = await api.get(`/medico/agendamentos?data=${dateStr}`)
            setAppointments(response.data)
        } catch (error) {
            toast.error('Erro ao carregar agendamentos')
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className={styles.container}>
            <DashboardHeader user={user} />
            <StatsCards appointments={appointments} />

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