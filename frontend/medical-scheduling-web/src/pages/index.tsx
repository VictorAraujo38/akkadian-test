import { useEffect } from 'react'
import { useRouter } from 'next/router'
import { useAuth } from '@/contexts/AuthContext'
import Head from 'next/head'
import { LoadingSpinner } from '@/components/Common/LoadingSpinner'

export default function Home() {
    const router = useRouter()
    const { user, loading } = useAuth()

    useEffect(() => {
        if (!loading) {
            if (!user) {
                router.push('/login')
            } else if (user.role === 'Patient') {
                router.push('/paciente/dashboard')
            } else if (user.role === 'Doctor') {
                router.push('/medico/dashboard')
            }
        }
    }, [user, loading, router])

    return (
        <>
            <Head>
                <title>Medical Scheduling - Sistema de Agendamento Médico</title>
                <meta name="description" content="Sistema completo para agendamento médico com triagem por IA" />
            </Head>
            <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
                <LoadingSpinner size="large" />
            </div>
        </>
    )
}