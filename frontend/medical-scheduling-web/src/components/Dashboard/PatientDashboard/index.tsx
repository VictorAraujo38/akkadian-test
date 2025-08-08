import React from 'react'
import styles from './PatientDashboard.module.css'
import { DashboardHeader } from './DashboardHeader'
import { AppointmentsList } from './components/AppointmentsList'
import { StatsCards } from './components/StatsCards'
import { usePatientDashboard } from './hooks/usePatientDashboard'

export const PatientDashboard: React.FC = () => {
    const {
        appointments,
        loading,
        filter,
        setFilter,
        stats
    } = usePatientDashboard()

    return (
        <div className={styles.container}>
            <DashboardHeader />
            <StatsCards stats={stats} />
            <AppointmentsList
                appointments={appointments}
                loading={loading}
                filter={filter}
                onFilterChange={setFilter}
            />
        </div>
    )
}