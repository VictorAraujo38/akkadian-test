import React, { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { useRouter } from 'next/router'
import { appointmentService } from '@/services/api'
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

    const onSubmit = async (data: AppointmentFormData) => {
        if (!selectedTime) {
            toast.error('Selecione um horário disponível')
            return
        }

        setLoading(true)
        try {
            const appointmentDateTime = new Date(`${data.appointmentDate}T${selectedTime}`)

            const response = await appointmentService.createAppointment({
                appointmentDate: appointmentDateTime.toISOString(),
                symptoms: data.symptoms,
                additionalNotes: data.additionalNotes,
            })

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
                            🤖 Analisar Sintomas com IA
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