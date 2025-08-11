import React from 'react'
import { format, addMonths, subMonths, startOfMonth, endOfMonth, eachDayOfInterval, isSameMonth, isSameDay, isToday, startOfWeek, endOfWeek, addWeeks, subWeeks } from 'date-fns'
import { ptBR } from 'date-fns/locale'

interface Patient {
  id: string
  name: string
  email: string
  phone: string
}

interface Appointment {
  id: string
  patientId?: string
  patientName: string
  doctorId: string
  appointmentDate: string
  symptoms: string
  status: 'Scheduled' | 'Completed' | 'Cancelled' | 'Agendado' | 'Concluído' | 'Cancelado'
  recommendedSpecialty?: string
}

type ViewMode = 'month' | 'week' | 'day'

interface AppointmentCalendarProps {
  appointments: Appointment[]
  selectedDate: Date
  onDateChange: (date: Date) => void
  viewMode: ViewMode
  onViewModeChange: (mode: ViewMode) => void
  loading: boolean
}

export const AppointmentCalendar: React.FC<AppointmentCalendarProps> = ({
  appointments,
  selectedDate,
  onDateChange,
  viewMode,
  onViewModeChange,
  loading
}) => {
  const handlePrevPeriod = () => {
    if (viewMode === 'month') {
      onDateChange(subMonths(selectedDate, 1))
    } else if (viewMode === 'week') {
      onDateChange(subWeeks(selectedDate, 1))
    } else {
      onDateChange(new Date(selectedDate.getTime() - 24 * 60 * 60 * 1000))
    }
  }

  const handleNextPeriod = () => {
    if (viewMode === 'month') {
      onDateChange(addMonths(selectedDate, 1))
    } else if (viewMode === 'week') {
      onDateChange(addWeeks(selectedDate, 1))
    } else {
      onDateChange(new Date(selectedDate.getTime() + 24 * 60 * 60 * 1000))
    }
  }

  const handleDayClick = (day: Date) => {
    onDateChange(day)
  }

  // Generate days for the current view
  const getDaysForView = () => {
    if (viewMode === 'month') {
      const monthStart = startOfMonth(selectedDate)
      const monthEnd = endOfMonth(selectedDate)
      return eachDayOfInterval({ start: monthStart, end: monthEnd })
    } else if (viewMode === 'week') {
      const weekStart = startOfWeek(selectedDate, { weekStartsOn: 0 })
      const weekEnd = endOfWeek(selectedDate, { weekStartsOn: 0 })
      return eachDayOfInterval({ start: weekStart, end: weekEnd })
    } else {
      return [selectedDate]
    }
  }

  const daysInView = getDaysForView()

  // Get appointments for the current view period
  const getAppointmentsForPeriod = () => {
    if (viewMode === 'day') {
      return appointments.filter(appointment => {
        const appointmentDate = new Date(appointment.appointmentDate)
        return isSameDay(appointmentDate, selectedDate)
      })
    } else if (viewMode === 'week') {
      const weekStart = startOfWeek(selectedDate, { weekStartsOn: 0 })
      const weekEnd = endOfWeek(selectedDate, { weekStartsOn: 0 })
      return appointments.filter(appointment => {
        const appointmentDate = new Date(appointment.appointmentDate)
        return appointmentDate >= weekStart && appointmentDate <= weekEnd
      })
    } else { // month
      const monthStart = startOfMonth(selectedDate)
      const monthEnd = endOfMonth(selectedDate)
      return appointments.filter(appointment => {
        const appointmentDate = new Date(appointment.appointmentDate)
        return appointmentDate >= monthStart && appointmentDate <= monthEnd
      })
    }
  }

  // Get appointments for each day
  const getAppointmentsForDay = (day: Date) => {
    return appointments.filter(appointment => {
      const appointmentDate = new Date(appointment.appointmentDate)
      return isSameDay(appointmentDate, day)
    })
  }

  const getPeriodTitle = () => {
    if (viewMode === 'day') {
      return format(selectedDate, "d 'de' MMMM 'de' yyyy", { locale: ptBR })
    } else if (viewMode === 'week') {
      const weekStart = startOfWeek(selectedDate, { weekStartsOn: 0 })
      const weekEnd = endOfWeek(selectedDate, { weekStartsOn: 0 })
      return `${format(weekStart, 'd MMM', { locale: ptBR })} - ${format(weekEnd, 'd MMM yyyy', { locale: ptBR })}`
    } else {
      return format(selectedDate, 'MMMM yyyy', { locale: ptBR })
    }
  }

  const periodAppointments = getAppointmentsForPeriod().sort((a, b) =>
    new Date(a.appointmentDate).getTime() - new Date(b.appointmentDate).getTime()
  )

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow p-4 h-full">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-lg font-semibold">Calendário de Consultas</h2>
          <div className="flex space-x-2">
            <button className="px-3 py-1 text-sm bg-gray-100 rounded-md">Mês</button>
            <button className="px-3 py-1 text-sm bg-gray-100 rounded-md">Semana</button>
            <button className="px-3 py-1 text-sm bg-gray-100 rounded-md">Dia</button>
          </div>
        </div>
        <div className="flex justify-center items-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
        </div>
      </div>
    )
  }

  return (
    <div className="bg-white rounded-lg shadow p-4 h-full">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-lg font-semibold">Calendário de Consultas</h2>
        <div className="flex space-x-2">
          <button
            className={`px-3 py-1 text-sm rounded-md ${viewMode === 'month' ? 'bg-blue-500 text-white' : 'bg-gray-100'}`}
            onClick={() => onViewModeChange('month')}
          >
            Mês
          </button>
          <button
            className={`px-3 py-1 text-sm rounded-md ${viewMode === 'week' ? 'bg-blue-500 text-white' : 'bg-gray-100'}`}
            onClick={() => onViewModeChange('week')}
          >
            Semana
          </button>
          <button
            className={`px-3 py-1 text-sm rounded-md ${viewMode === 'day' ? 'bg-blue-500 text-white' : 'bg-gray-100'}`}
            onClick={() => onViewModeChange('day')}
          >
            Dia
          </button>
        </div>
      </div>

      <div className="mb-4 flex justify-between items-center">
        <button
          onClick={handlePrevPeriod}
          className="p-2 rounded-full hover:bg-gray-100"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clipRule="evenodd" />
          </svg>
        </button>
        <h3 className="text-lg font-medium">
          {getPeriodTitle()}
        </h3>
        <button
          onClick={handleNextPeriod}
          className="p-2 rounded-full hover:bg-gray-100"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clipRule="evenodd" />
          </svg>
        </button>
      </div>

      {viewMode === 'month' && (
        <div className="grid grid-cols-7 gap-1 mb-4">
          {['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'].map((day) => (
            <div key={day} className="text-center text-sm font-medium py-2">
              {day}
            </div>
          ))}

          {Array(startOfMonth(selectedDate).getDay())
            .fill(null)
            .map((_, index) => (
              <div key={`empty-${index}`} className="h-12 p-1"></div>
            ))}

          {daysInView.map((day) => {
            const dayAppointments = getAppointmentsForDay(day)
            const isSelected = isSameDay(day, selectedDate)
            const isTodayDate = isToday(day)

            return (
              <div
                key={day.toString()}
                className={`h-12 p-1 relative cursor-pointer ${isSelected ? 'bg-blue-100 rounded' : ''}`}
                onClick={() => handleDayClick(day)}
              >
                <div
                  className={`flex justify-center items-center h-6 w-6 ${isTodayDate
                      ? 'bg-gradient-to-r from-orange-500 to-red-500 text-white rounded-full font-bold shadow-lg ring-2 ring-orange-300'
                      : ''
                    }`}
                >
                  {format(day, 'd')}
                </div>
                {dayAppointments.length > 0 && (
                  <div className="absolute bottom-1 right-1 h-2 w-2 bg-blue-500 rounded-full"></div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {/* Lista de agendamentos do período */}
      <div className="border-t pt-4">
        <h4 className="font-medium mb-3 flex items-center gap-2">
          📅 Consultas
          {viewMode === 'month' && ' do Mês'}
          {viewMode === 'week' && ' da Semana'}
          {viewMode === 'day' && ' do Dia'}
          <span className="text-sm text-gray-500">({periodAppointments.length})</span>
        </h4>

        <div className="space-y-3 max-h-64 overflow-y-auto">
          {periodAppointments.length === 0 ? (
            <p className="text-gray-500 text-sm text-center py-4">
              Não há consultas agendadas para este período.
            </p>
          ) : (
            periodAppointments.map((appointment) => {
              const appointmentDate = new Date(appointment.appointmentDate)
              const dayOfWeek = format(appointmentDate, 'EEEE', { locale: ptBR })
              const dayMonth = format(appointmentDate, "d 'de' MMM", { locale: ptBR })
              const time = format(appointmentDate, 'HH:mm')

              return (
                <div
                  key={appointment.id}
                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors cursor-pointer"
                  onClick={() => onDateChange(appointmentDate)}
                >
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <div className="font-medium text-gray-900">{appointment.patientName}</div>
                      <span className={`px-2 py-1 text-xs rounded-full ${appointment.status === 'Scheduled' || appointment.status === 'Agendado' ? 'bg-yellow-100 text-yellow-800' :
                          appointment.status === 'Completed' || appointment.status === 'Concluído' ? 'bg-green-100 text-green-800' :
                            'bg-red-100 text-red-800'
                        }`}>
                        {appointment.status === 'Scheduled' || appointment.status === 'Agendado' ? 'Agendado' :
                          appointment.status === 'Completed' || appointment.status === 'Concluído' ? 'Concluído' :
                            'Cancelado'}
                      </span>
                    </div>
                    <div className="text-sm text-gray-600">
                      {viewMode !== 'day' && (
                        <span className="font-medium">{dayOfWeek}, {dayMonth} às </span>
                      )}
                      <span className="font-medium text-blue-600">{time}</span>
                    </div>
                    {appointment.recommendedSpecialty && (
                      <div className="text-xs text-gray-500 mt-1">
                        📋 {appointment.recommendedSpecialty}
                      </div>
                    )}
                    <div className="text-xs text-gray-400 mt-1 line-clamp-1">
                      {appointment.symptoms}
                    </div>
                  </div>
                  <div className="ml-2">
                    <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                    </svg>
                  </div>
                </div>
              )
            })
          )}
        </div>
      </div>
    </div>
  )
}