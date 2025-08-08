import React from 'react'
import { CalendarDaysIcon, ClipboardDocumentListIcon } from '@heroicons/react/24/outline'
import styles from '../DoctorDashboard.module.css'

interface DashboardHeaderProps {
    user: any
}

export const DashboardHeader: React.FC<DashboardHeaderProps> = ({ user }) => {
    const getGreeting = () => {
        const hour = new Date().getHours()
        if (hour < 12) return 'Bom dia'
        if (hour < 18) return 'Boa tarde'
        return 'Boa noite'
    }

    return (
        <div className="bg-gradient-to-r from-blue-600 to-purple-600 rounded-xl p-6 text-white mb-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold mb-2">
                        {getGreeting()}, Dr(a). {user?.name}!
                    </h1>
                    <p className="text-blue-100">
                        {format(new Date(), "EEEE, dd 'de' MMMM 'de' yyyy", { locale: ptBR })}
                    </p>
                </div>
                <div className="flex gap-3">
                    <button className="bg-white/20 backdrop-blur-sm px-4 py-2 rounded-lg hover:bg-white/30 transition-colors flex items-center gap-2">
                        <CalendarDaysIcon className="h-5 w-5" />
                        Agenda Completa
                    </button>
                    <button className="bg-white/20 backdrop-blur-sm px-4 py-2 rounded-lg hover:bg-white/30 transition-colors flex items-center gap-2">
                        <ClipboardDocumentListIcon className="h-5 w-5" />
                        Relatórios
                    </button>
                </div>
            </div>
        </div>
    )
}