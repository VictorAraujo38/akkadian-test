import React, { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { useRouter } from 'next/router'
import { api, appointmentService } from '@/services/api'
import { toast } from 'react-toastify'
import { CalendarIcon, ClockIcon, DocumentTextIcon, ExclamationTriangleIcon } from '@heroicons/react/24/outline'
import styles from './AppointmentForm.module.css'
import { LoadingSpinner } from '@/components/Common/LoadingSpinner'

type AppointmentFormData = {
    appointmentDate: string
    appointmentTime: string
    symptoms: string
    additionalNotes?: string
}

export const AppointmentForm: React.FC = () => {
    const router = useRouter()
    const [loading, setLoading] = useState(false)
    const [selectedTime, setSelectedTime] = useState('')
    const [triageResult, setTriageResult] = useState<any>(null)
    const [availableSlots, setAvailableSlots] = useState<string[]>([])
    const [loadingSlots, setLoadingSlots] = useState(false)
    const [selectedDate, setSelectedDate] = useState('')
    const [showDoctorSelection, setShowDoctorSelection] = useState(false)
    const [availableDoctors, setAvailableDoctors] = useState<any[]>([])
    const [selectedDoctorId, setSelectedDoctorId] = useState<number | null>(null)
    const [appointmentCreationAttempted, setAppointmentCreationAttempted] = useState(false)

    const {
        register,
        handleSubmit,
        watch,
        setValue,
        formState: { errors },
    } = useForm<AppointmentFormData>()

    const symptoms = watch('symptoms')
    const appointmentDate = watch('appointmentDate')

    // Buscar horários disponíveis quando a data muda
    useEffect(() => {
        if (appointmentDate) {
            fetchAvailableSlots(appointmentDate)
            setSelectedTime('') // Reset selected time
            setValue('appointmentTime', '') // Reset form value
        }
    }, [appointmentDate, setValue])

    const fetchAvailableSlots = async (date: string) => {
        setLoadingSlots(true)
        try {
            const response = await appointmentService.getAvailableTimeSlots(date)
            const slots = response.map((datetime: string) => {
                const time = new Date(datetime).toLocaleTimeString('pt-BR', {
                    hour: '2-digit',
                    minute: '2-digit'
                })
                return time
            })
            setAvailableSlots(slots)
        } catch (error) {
            toast.error('Erro ao carregar horários disponíveis')
            setAvailableSlots([])
        } finally {
            setLoadingSlots(false)
        }
    }

    const fetchAvailableDoctors = async (date: string, time: string) => {
        try {
            const appointmentDateTime = new Date(`${date}T${time}`)

            // Buscar todas as especialidades e seus médicos
            const specialtiesResponse = await api.get('/specialties/with-doctors', {
                params: { date: appointmentDateTime.toISOString().split('T')[0] }
            })

            const doctors: any[] = []
            specialtiesResponse.data.forEach((specialty: any) => {
                specialty.Doctors.forEach((doctor: any) => {
                    doctors.push({
                        ...doctor,
                        specialtyName: specialty.Name,
                        specialtyDepartment: specialty.Department
                    })
                })
            })

            setAvailableDoctors(doctors)
        } catch (error) {
            console.error('Erro ao buscar médicos:', error)
            setAvailableDoctors([])
        }
    }

    const onSubmit = async (data: AppointmentFormData) => {
        if (!selectedTime) {
            toast.error('Selecione um horário disponível')
            return
        }

        setLoading(true)
        setAppointmentCreationAttempted(true)

        try {
            const appointmentDateTime = new Date(`${data.appointmentDate}T${selectedTime}`)

            const appointmentData: any = {
                appointmentDate: appointmentDateTime.toISOString(),
                symptoms: data.symptoms,
                preferredSpecialty: triageResult?.recommendedSpecialty || "Clínica Geral",
                additionalNotes: data.additionalNotes,
            }

            // Se um médico foi selecionado manualmente, adicionar ao payload
            if (selectedDoctorId) {
                appointmentData.preferredDoctorId = selectedDoctorId
            }

            const response = await appointmentService.createAppointment(appointmentData)

            // Verificar se médico foi atribuído
            if (!response.doctorName || response.doctorName === 'Nome não disponível') {
                // Médico não foi atribuído automaticamente
                await fetchAvailableDoctors(data.appointmentDate, selectedTime)
                setShowDoctorSelection(true)

                toast.warning('Nenhum médico da especialidade recomendada está disponível neste horário. Selecione um médico manualmente.')
                return
            }

            toast.success('Agendamento criado com sucesso!')

            // Mostrar informações do agendamento
            if (response.doctorName) {
                toast.info(`Médico atribuído: ${response.doctorName}`)
            }
            if (response.recommendedSpecialty) {
                toast.info(`Especialidade: ${response.recommendedSpecialty}`)
            }

            router.push('/patient/dashboard')
        } catch (error: any) {
            const errorMessage = error.response?.data?.message || error.message || 'Erro ao criar agendamento'
            toast.error(errorMessage)
        } finally {
            setLoading(false)
        }
    }

    const confirmAppointmentWithSelectedDoctor = async () => {
        if (!selectedDoctorId) {
            toast.error('Selecione um médico')
            return
        }

        const data = watch() // Pegar dados do formulário

        setLoading(true)
        try {
            const appointmentDateTime = new Date(`${data.appointmentDate}T${selectedTime}`)

            const response = await appointmentService.createAppointment({
                appointmentDate: appointmentDateTime.toISOString(),
                symptoms: data.symptoms,
                preferredSpecialty: triageResult?.recommendedSpecialty || "Clínica Geral",
                additionalNotes: data.additionalNotes,
                preferredDoctorId: selectedDoctorId
            })

            toast.success('Agendamento criado com sucesso!')

            if (response.doctorName) {
                toast.info(`Médico atribuído: ${response.doctorName}`)
            }

            router.push('/patient/dashboard')
        } catch (error: any) {
            const errorMessage = error.response?.data?.message || error.message || 'Erro ao criar agendamento'
            toast.error(errorMessage)
        } finally {
            setLoading(false)
            setShowDoctorSelection(false)
        }
    }

    const handleSymptomAnalysis = async () => {
        if (!symptoms || symptoms.length < 10) {
            toast.warning('Descreva seus sintomas com pelo menos 10 caracteres')
            return
        }

        try {
            const response = await appointmentService.getTriageRecommendation(symptoms)
            setTriageResult(response)

            let message = `Especialidade recomendada: ${response.recommendedSpecialty}`
            if (response.confidence) {
                message += ` (Confiança: ${response.confidence})`
            }

            toast.success(message)
        } catch (error) {
            toast.error('Erro ao analisar sintomas')
        }
    }

    const handleTimeSelection = (time: string) => {
        setSelectedTime(time)
        setValue('appointmentTime', time)
    }

    // Set minimum date to tomorrow
    const tomorrow = new Date()
    tomorrow.setDate(tomorrow.getDate() + 1)
    const minDate = tomorrow.toISOString().split('T')[0]

    // Set maximum date to 30 days from now
    const maxDate = new Date()
    maxDate.setDate(maxDate.getDate() + 30)
    const maxDateStr = maxDate.toISOString().split('T')[0]

    return (
        <div className={styles.container}>
            <form onSubmit={handleSubmit(onSubmit)} className={styles.form}>
                <h2 className={styles.title}>
                    <CalendarIcon className="h-6 w-6" />
                    Novo Agendamento
                </h2>

                <div className={styles.section}>
                    <h3 className={styles.sectionTitle}>
                        <CalendarIcon className="h-5 w-5" />
                        Data e Horário
                    </h3>

                    <div className={styles.dateTimeGrid}>
                        <div className={styles.inputGroup}>
                            <label className={styles.label}>Data do agendamento</label>
                            <input
                                {...register('appointmentDate', { required: 'Data é obrigatória' })}
                                type="date"
                                min={minDate}
                                max={maxDateStr}
                                className={styles.input}
                            />
                            {errors.appointmentDate && (
                                <span className={styles.error}>{errors.appointmentDate.message}</span>
                            )}
                            <p className="text-sm text-gray-500 mt-1">
                                Agendamentos de segunda a sexta-feira, com 2h de antecedência
                            </p>
                        </div>

                        <div className={styles.inputGroup}>
                            <label className={styles.label}>
                                Horários disponíveis {appointmentDate && `(${new Date(appointmentDate).toLocaleDateString('pt-BR')})`}
                            </label>

                            {!appointmentDate && (
                                <div className="text-gray-500 text-sm p-4 border-2 border-dashed border-gray-300 rounded-lg">
                                    Selecione uma data para ver os horários disponíveis
                                </div>
                            )}

                            {appointmentDate && loadingSlots && (
                                <div className="flex justify-center p-4">
                                    <LoadingSpinner size="small" />
                                </div>
                            )}

                            {appointmentDate && !loadingSlots && (
                                <div className={styles.timeSlots}>
                                    {availableSlots.length === 0 ? (
                                        <div className="col-span-full text-center p-4 text-gray-500 border-2 border-dashed border-gray-300 rounded-lg">
                                            <ExclamationTriangleIcon className="h-6 w-6 mx-auto mb-2" />
                                            Não há horários disponíveis para esta data
                                        </div>
                                    ) : (
                                        availableSlots.map((time) => (
                                            <button
                                                key={time}
                                                type="button"
                                                onClick={() => handleTimeSelection(time)}
                                                className={`${styles.timeSlot} ${selectedTime === time ? styles.timeSlotSelected : ''}`}
                                            >
                                                <ClockIcon className="h-4 w-4" />
                                                {time}
                                            </button>
                                        ))
                                    )}
                                </div>
                            )}

                            <input
                                type="hidden"
                                {...register('appointmentTime', {
                                    required: 'Horário é obrigatório',
                                    validate: () => selectedTime !== '' || 'Selecione um horário disponível'
                                })}
                                value={selectedTime}
                            />
                            {errors.appointmentTime && (
                                <span className={styles.error}>{errors.appointmentTime.message}</span>
                            )}
                        </div>
                    </div>
                </div>

                <div className={styles.section}>
                    <h3 className={styles.sectionTitle}>
                        <DocumentTextIcon className="h-5 w-5" />
                        Informações Médicas
                    </h3>

                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Descreva seus sintomas</label>
                        <textarea
                            {...register('symptoms', {
                                required: 'Sintomas são obrigatórios',
                                minLength: {
                                    value: 10,
                                    message: 'Descreva os sintomas com pelo menos 10 caracteres',
                                },
                            })}
                            rows={4}
                            className={styles.textarea}
                            placeholder="Ex: Dor de cabeça frequente há 3 dias, acompanhada de náusea e sensibilidade à luz..."
                        />
                        {errors.symptoms && (
                            <span className={styles.error}>{errors.symptoms.message}</span>
                        )}

                        <button
                            type="button"
                            onClick={handleSymptomAnalysis}
                            className={styles.analyzeButton}
                            disabled={!symptoms || symptoms.length < 10}
                        >
                            Analisar Sintomas com IA
                        </button>

                        {triageResult && (
                            <div className={styles.triageResult}>
                                <div className="flex items-start gap-2">
                                    <div className="flex-shrink-0">
                                        <div className="w-2 h-2 bg-green-500 rounded-full mt-2"></div>
                                    </div>
                                    <div>
                                        <p className="font-semibold">
                                            Especialidade Recomendada: {triageResult.recommendedSpecialty}
                                        </p>
                                        {triageResult.confidence && (
                                            <p className="text-sm">
                                                Confiança: {triageResult.confidence}
                                            </p>
                                        )}
                                        {triageResult.reasoning && (
                                            <p className="text-sm mt-1">
                                                {triageResult.reasoning}
                                            </p>
                                        )}
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>

                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Observações adicionais (opcional)</label>
                        <textarea
                            {...register('additionalNotes')}
                            rows={3}
                            className={styles.textarea}
                            placeholder="Medicamentos em uso, alergias, informações relevantes..."
                        />
                    </div>
                </div>

                {showDoctorSelection && (
                    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                        <div className="bg-white rounded-lg p-6 max-w-2xl w-full mx-4 max-h-[80vh] overflow-y-auto">
                            <h3 className="text-lg font-semibold mb-4">
                                 Selecione um médico disponível
                            </h3>

                            <p className="text-gray-600 mb-4">
                                Não encontramos um médico da especialidade recomendada ({triageResult?.recommendedSpecialty}) disponível neste horário.
                                Selecione um dos médicos disponíveis abaixo:
                            </p>

                            <div className="space-y-3 mb-6">
                                {availableDoctors.length === 0 ? (
                                    <p className="text-center text-gray-500 py-8">
                                        Nenhum médico disponível neste horário
                                    </p>
                                ) : (
                                    availableDoctors.map((doctor) => (
                                        <div
                                            key={doctor.Id}
                                            className={`border rounded-lg p-4 cursor-pointer transition-all ${selectedDoctorId === doctor.Id
                                                    ? 'border-blue-500 bg-blue-50'
                                                    : 'border-gray-200 hover:border-gray-300'
                                                }`}
                                            onClick={() => setSelectedDoctorId(doctor.Id)}
                                        >
                                            <div className="flex items-center justify-between">
                                                <div>
                                                    <h4 className="font-medium text-gray-900">
                                                        Dr(a). {doctor.Name}
                                                    </h4>
                                                    <p className="text-sm text-blue-600">
                                                         {doctor.specialtyName}
                                                    </p>
                                                    <p className="text-xs text-gray-500">
                                                         {doctor.specialtyDepartment}
                                                    </p>
                                                    {doctor.CrmNumber && (
                                                        <p className="text-xs text-gray-400">
                                                             {doctor.CrmNumber}
                                                        </p>
                                                    )}
                                                </div>
                                                <div className="flex items-center">
                                                    {selectedDoctorId === doctor.Id && (
                                                        <div className="w-5 h-5 bg-blue-500 rounded-full flex items-center justify-center">
                                                            <div className="w-2 h-2 bg-white rounded-full"></div>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    ))
                                )}
                            </div>

                            <div className="flex justify-end gap-3">
                                <button
                                    type="button"
                                    onClick={() => {
                                        setShowDoctorSelection(false)
                                        setSelectedDoctorId(null)
                                    }}
                                    className="px-4 py-2 text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50"
                                >
                                    Cancelar
                                </button>
                                <button
                                    type="button"
                                    onClick={confirmAppointmentWithSelectedDoctor}
                                    disabled={!selectedDoctorId || loading}
                                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
                                >
                                    {loading ? <LoadingSpinner size="small" /> : ' Confirmar Agendamento'}
                                </button>
                            </div>
                        </div>
                    </div>
                )}

                <div className={styles.actions}>
                    <button
                        type="button"
                        onClick={() => router.back()}
                        className={styles.cancelButton}
                    >
                        Cancelar
                    </button>
                    <button
                        type="submit"
                        disabled={loading || !selectedTime}
                        className={styles.submitButton}
                    >
                        {loading ? <LoadingSpinner size="small" /> : 'Confirmar Agendamento'}
                    </button>
                </div>
            </form>
        </div>
    )
}