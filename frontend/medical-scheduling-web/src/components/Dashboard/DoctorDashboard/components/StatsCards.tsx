import React from 'react'
import {
    UserGroupIcon,
    ClockIcon,
    CheckCircleIcon,
    ExclamationCircleIcon
} from '@heroicons/react/24/outline'

interface StatsCardsProps {
    appointments: any[]
}

export const StatsCards: React.FC<StatsCardsProps> = ({ appointments }) => {
    const stats = {
        total: appointments.length,
        pending: appointments.filter(a => a.status === 'pending').length,
        completed: appointments.filter(a => a.status === 'completed').length,
        nextAppointment: appointments.find(a => new Date(a.appointmentDate) > new Date()),
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
                            {stats.nextAppointment
                                ? format(new Date(stats.nextAppointment.appointmentDate), 'HH:mm')
                                : 'Sem consultas'}
                        </p>
                    </div>
                    <ExclamationCircleIcon className="h-8 w-8 text-purple-500" />
                </div>
            </div>
        </div>
    )
}