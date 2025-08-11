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
                        <div key={index} className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                            <div className="flex justify-between items-start">
                                <div className="flex-1">
                                    <div className="flex items-center gap-2 mb-2">
                                        {appointment.doctorName && appointment.doctorName !== 'Nome não disponível' ? (
                                            <h3 className="font-medium text-gray-800">
                                                Dr(a). {appointment.doctorName}
                                            </h3>
                                        ) : (
                                            <h3 className="font-medium text-orange-600">
                                                Dr(a). Nome não disponível
                                            </h3>
                                        )}
                                        {appointment.doctorCrm && (
                                            <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full">
                                                {appointment.doctorCrm}
                                            </span>
                                        )}
                                    </div>

                                    <div className="space-y-1">
                                        {appointment.recommendedSpecialty && appointment.recommendedSpecialty !== 'Especialidade não informada' ? (
                                            <p className="text-sm text-blue-600 font-medium">
                                                📋 {appointment.recommendedSpecialty}
                                            </p>
                                        ) : (
                                            <p className="text-sm text-orange-500">
                                                📋 Especialidade não informada
                                            </p>
                                        )}

                                        <p className="text-sm text-gray-600">
                                            🗓️ {new Date(appointment.appointmentDate).toLocaleDateString('pt-BR')} às{' '}
                                            {new Date(appointment.appointmentDate).toLocaleTimeString('pt-BR', {
                                                hour: '2-digit',
                                                minute: '2-digit'
                                            })}
                                        </p>

                                        {appointment.symptoms && (
                                            <p className="text-sm text-gray-500 mt-2">
                                                💬 <span className="font-medium">Sintomas:</span> {appointment.symptoms}
                                            </p>
                                        )}

                                        {appointment.triageReasoning && (
                                            <p className="text-xs text-gray-400 mt-1 italic">
                                                🤖 {appointment.triageReasoning}
                                            </p>
                                        )}
                                    </div>
                                </div>

                                <div className="ml-4">
                                    <span className={`px-3 py-1 rounded-full text-xs font-medium ${appointment.status === 'Completed' || appointment.status === 'completed' ? 'bg-green-100 text-green-800' :
                                            appointment.status === 'Scheduled' || appointment.status === 'pending' ? 'bg-yellow-100 text-yellow-800' :
                                                appointment.status === 'Cancelled' || appointment.status === 'cancelled' ? 'bg-red-100 text-red-800' :
                                                    'bg-gray-100 text-gray-800'
                                        }`}>
                                        {appointment.status === 'Completed' || appointment.status === 'completed' ? ' Concluída' :
                                            appointment.status === 'Scheduled' || appointment.status === 'pending' ? ' Agendada' :
                                                appointment.status === 'Cancelled' || appointment.status === 'cancelled' ? ' Cancelada' :
                                                    appointment.status}
                                    </span>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    )
}