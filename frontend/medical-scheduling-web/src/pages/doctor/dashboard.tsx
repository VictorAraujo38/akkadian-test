import { NextPage } from 'next'
import { DoctorDashboard } from '@/components/Dashboard/DoctorDashboard'
import { withAuth } from '@/hooks/withAuth'
import Head from 'next/head'

const DoctorDashboardPage: NextPage = () => {
    return (
        <>
            <Head>
                <title>Dashboard do Médico - Medical Scheduling</title>
                <meta name="description" content="Gerencie suas consultas e pacientes" />
            </Head>
            <DoctorDashboard />
        </>
    )
}

export default withAuth(DoctorDashboardPage, 'Doctor')