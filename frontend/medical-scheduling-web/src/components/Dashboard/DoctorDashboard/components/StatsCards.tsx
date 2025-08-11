import React from 'react'
import {
    UserGroupIcon,
    ClockIcon,
    CheckCircleIcon,
    ExclamationCircleIcon
} from '@heroicons/react/24/outline'
import { format } from 'date-fns'

interface StatsCardsProps {
    appointments: any[]
    selectedDate: Date
}

export const StatsCards: React.FC<StatsCardsProps> = ({ appointments, selectedDate }) => {
    // Filtrar agendamentos para o dia selecionado
    const todayAppointments = appointments.filter(appointment => {
        const appointmentDate = new Date(appointment.appointmentDate)
        return appointmentDate.toDateString() === selectedDate.toDateString()
    })

    // Calcular estatísticas reais
    const stats = {
        total: todayAppointments.length,
        pending: todayAppointments.filter(a => a.status === 'Scheduled' || a.status === 'Agendado').length,
        completed: todayAppointments.filter(a => a.status === 'Completed' || a.status === 'Concluído').length,
        nextAppointment: todayAppointments
            .filter(a => new Date(a.appointmentDate) > new Date())
            .sort((a, b) => new Date(a.appointmentDate).getTime() - new Date(b.appointmentDate).getTime())[0]
    }

    const getNextAppointmentTime = () => {
        if (!stats.nextAppointment) return 'Sem consultas'
        
        const appointmentTime = new Date(stats.nextAppointment.appointmentDate)
        const now = new Date()
        const diffMs = appointmentTime.getTime() - now.getTime()
        
        if (diffMs <= 0) return 'Agora'
        
        const diffMins = Math.floor(diffMs / (1000 * 60))
        const diffHours = Math.floor(diffMins / 60)
        
        if (diffHours > 0) {
            return `${diffHours}h ${diffMins % 60}min`
        } else {
            return `${diffMins} min`
        }
    }

    return (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
            <div className="bg-white rounded-lg p-4 shadow-sm border-l-4 border-blue-500">
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-gray-500 text-sm">Total Hoje</p>
                        <p className="text-2xl font-bold text-gray-800">{stats.total}</p>
                    </div>
                    <UserGroupIcon className="h-8 w-8 text-blue-500" />
                </div>
            </div>

            <div className="bg-white rounded-lg p-4 shadow-sm border-l-4 border-yellow-500">
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-gray-500 text-sm">Aguardando</p>
                        <p className="text-2xl font-bold text-gray-800">{stats.pending}</p>
                    </div>
                    <ClockIcon className="h-8 w-8 text-yellow-500" />
                </div>
            </div>

            <div className="bg-white rounded-lg p-4 shadow-sm border-l-4 border-green-500">
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-gray-500 text-sm">Atendidos</p>
                        <p className="text-2xl font-bold text-gray-800">{stats.completed}</p>
                    </div>
                    <CheckCircleIcon className="h-8 w-8 text-green-500" />
                </div>
            </div>

            <div className="bg-white rounded-lg p-4 shadow-sm border-l-4 border-purple-500">
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-gray-500 text-sm">Próximo em</p>
                        <p className="text-lg font-bold text-gray-800">
                            {getNextAppointmentTime()}
                        </p>
                    </div>
                    <ExclamationCircleIcon className="h-8 w-8 text-purple-500" />
                </div>
            </div>
        </div>
    )
}