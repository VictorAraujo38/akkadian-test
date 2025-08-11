import { NextPage } from 'next'
import { PatientDashboard } from '@/components/Dashboard/PatientDashboard'
import { withAuth } from '@/hooks/withAuth'
import Head from 'next/head'

const PatientDashboardPage: NextPage = () => {
    return (
        <>
            <Head>
                <title>Dashboard do Paciente - Medical Scheduling</title>
                <meta name="description" content="Gerencie seus agendamentos médicos" />
            </Head>
            <PatientDashboard />
        </>
    )
}

export default withAuth(PatientDashboardPage, 'Patient')