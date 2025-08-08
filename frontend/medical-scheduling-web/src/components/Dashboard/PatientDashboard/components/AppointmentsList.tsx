import React from 'react'

interface AppointmentsListProps {
    appointments: any[]
    loading: boolean
    filter: 'all' | 'upcoming' | 'past'
    onFilterChange: (filter: 'all' | 'upcoming' | 'past') => void
}

export const AppointmentsList: React.FC<AppointmentsListProps> = ({
    appointments,
    loading,
    filter,
    onFilterChange
}) => {
    if (loading) {
        return (
            <div className="bg-white rounded-lg p-6 shadow-sm">
                <p className="text-gray-500">Carregando consultas...</p>
            </div>
        )
    }

    return (
        <div className="bg-white rounded-lg p-6 shadow-sm">
            <div className="flex justify-between items-center mb-4">
                <h2 className="text-xl font-semibold text-gray-800">Suas Consultas</h2>
                <select
                    value={filter}
                    onChange={(e) => onFilterChange(e.target.value as 'all' | 'upcoming' | 'past')}
                    className="border border-gray-300 rounded-md px-3 py-1 text-sm"
                >
                    <option value="all">Todas</option>
                    <option value="upcoming">Próximas</option>
                    <option value="past">Passadas</option>
                </select>
            </div>
            
            {appointments.length === 0 ? (
                <p className="text-gray-500 text-center py-8">Nenhuma consulta encontrada.</p>
            ) : (
                <div className="space-y-4">
                    {appointments.map((appointment, index) => (
                        <div key={index} className="border border-gray-200 rounded-lg p-4">
                            <div className="flex justify-between items-start">
                                <div>
                                    <h3 className="font-medium text-gray-800">
                                        Dr(a). {appointment.doctorName || 'Nome não disponível'}
                                    </h3>
                                    <p className="text-sm text-gray-600">
                                        {appointment.specialty || 'Especialidade não informada'}
                                    </p>
                                    <p className="text-sm text-gray-500">
                                        {new Date(appointment.appointmentDate).toLocaleDateString('pt-BR')} às{' '}
                                        {new Date(appointment.appointmentDate).toLocaleTimeString('pt-BR', {
                                            hour: '2-digit',
                                            minute: '2-digit'
                                        })}
                                    </p>
                                </div>
                                <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                                    appointment.status === 'completed' ? 'bg-green-100 text-green-800' :
                                    appointment.status === 'pending' ? 'bg-yellow-100 text-yellow-800' :
                                    appointment.status === 'cancelled' ? 'bg-red-100 text-red-800' :
                                    'bg-gray-100 text-gray-800'
                                }`}>
                                    {appointment.status === 'completed' ? 'Concluída' :
                                     appointment.status === 'pending' ? 'Pendente' :
                                     appointment.status === 'cancelled' ? 'Cancelada' :
                                     appointment.status}
                                </span>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    )
}