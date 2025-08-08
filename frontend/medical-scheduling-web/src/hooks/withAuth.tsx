import { useEffect } from 'react'
import { useRouter } from 'next/router'
import { useAuth } from '@/contexts/AuthContext'
import Layout from '@/components/Layout'

export const withAuth = (Component: React.FC, requiredRole?: string) => {
    return function ProtectedRoute(props: Record<string, unknown>) {
        const router = useRouter()
        const { user, loading } = useAuth()

        useEffect(() => {
            if (!loading) {
                if (!user) {
                    router.push('/login')
                } else if (requiredRole && user.role !== requiredRole) {
                    router.push('/')
                }
            }
        }, [user, loading, router])

        if (loading) {
            return (
                <div className="min-h-screen flex items-center justify-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
                </div>
            )
        }

        if (!user || (requiredRole && user.role !== requiredRole)) {
            return null
        }

        return (
            <Layout>
                <Component {...props} />
            </Layout>
        )
    }
}