import { useState, type FormEvent } from 'react';
import type { DailyStatus, SaveDailyStatus } from '@/core/models/types';
import { SelectField, TextAreaField, TextField } from '@/shared/components/Fields';
import { Modal } from '@/shared/components/Modal';
import { errorMessage } from '@/shared/utils/format';

interface Props {
  open: boolean;
  statusItem?: DailyStatus | null;
  prefilledEmployee?: { id: number; name: string } | null;
  defaultDate?: string;
  defaultProject?: string;
  onClose: () => void;
  onSave: (entry: SaveDailyStatus, id?: number) => Promise<void>;
}

const STATUS_OPTIONS = [
  'In Progress',
  'Completed',
  'Under Review',
  'Blocked',
  'On Hold',
];

export function DailyStatusFormModal({
  open,
  statusItem,
  prefilledEmployee,
  defaultDate,
  defaultProject,
  onClose,
  onSave,
}: Props) {
  const todayStr = defaultDate || new Date().toISOString().split('T')[0];

  const [date, setDate] = useState(
    statusItem ? statusItem.date.split('T')[0] : todayStr
  );
  const [projectOrTeamName, setProjectOrTeamName] = useState(
    statusItem?.projectOrTeamName ?? defaultProject ?? ''
  );
  const [workItemId, setWorkItemId] = useState(statusItem?.workItemId ?? '');
  const [taskActivity, setTaskActivity] = useState(statusItem?.taskActivity ?? '');
  const [plannedWorkForToday, setPlannedWorkForToday] = useState(
    statusItem?.plannedWorkForToday ?? ''
  );
  const [workCompletedProgress, setWorkCompletedProgress] = useState(
    statusItem?.workCompletedProgress ?? ''
  );
  const [status, setStatus] = useState(statusItem?.status ?? 'In Progress');
  const [hoursSpent, setHoursSpent] = useState<number | string>(
    statusItem?.hoursSpent ?? 8
  );
  const [blockerDependency, setBlockerDependency] = useState(
    statusItem?.blockerDependency ?? ''
  );
  const [supportRequired, setSupportRequired] = useState(
    statusItem?.supportRequired ?? ''
  );
  const [reviewerLead, setReviewerLead] = useState(
    statusItem?.reviewerLead ?? ''
  );
  const [remarks, setRemarks] = useState(statusItem?.remarks ?? '');

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!taskActivity.trim()) {
      setError('Task / Activity is required.');
      return;
    }
    if (!plannedWorkForToday.trim()) {
      setError('Planned Work for Today is required.');
      return;
    }

    setBusy(true);
    setError(null);

    const entryDate: string = (date && date.trim()) || todayStr || '';

    try {
      await onSave(
        {
          date: entryDate,
          employeeId: prefilledEmployee?.id,
          projectOrTeamName: projectOrTeamName.trim() || undefined,
          workItemId: workItemId.trim() || undefined,
          taskActivity: taskActivity.trim(),
          plannedWorkForToday: plannedWorkForToday.trim(),
          workCompletedProgress: workCompletedProgress.trim() || plannedWorkForToday.trim(),
          status,
          hoursSpent: Number(hoursSpent) || 0,
          blockerDependency: blockerDependency.trim() || undefined,
          supportRequired: supportRequired.trim() || undefined,
          reviewerLead: reviewerLead.trim() || undefined,
          remarks: remarks.trim() || undefined,
        },
        statusItem?.id
      );
      onClose();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      open={open}
      title={
        statusItem
          ? 'Edit Daily Status Entry'
          : prefilledEmployee
          ? `Log Daily Status for ${prefilledEmployee.name}`
          : 'Log Daily Status'
      }
      onClose={onClose}
    >
      <form onSubmit={handleSubmit} className="form">
        {error && (
          <div className="alert alert--danger" role="alert">
            {error}
          </div>
        )}

        <div className="grid grid--2">
          <TextField
            label="Date *"
            type="date"
            required
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />

          <TextField
            label="Project / Team"
            placeholder="e.g. Frontend App, Mobile, QA"
            value={projectOrTeamName}
            onChange={(e) => setProjectOrTeamName(e.target.value)}
          />
        </div>

        <div className="grid grid--3">
          <TextField
            label="Work Item ID"
            placeholder="e.g. PROJ-1024 / JIRA-42"
            value={workItemId}
            onChange={(e) => setWorkItemId(e.target.value)}
          />

          <SelectField
            label="Status *"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
          >
            {STATUS_OPTIONS.map((st) => (
              <option key={st} value={st}>
                {st}
              </option>
            ))}
          </SelectField>

          <TextField
            label="Hours Spent"
            type="number"
            step="0.5"
            min="0"
            max="24"
            value={hoursSpent}
            onChange={(e) => setHoursSpent(e.target.value)}
          />
        </div>

        <TextField
          label="Task / Activity *"
          required
          placeholder="Brief title or task summary"
          value={taskActivity}
          onChange={(e) => setTaskActivity(e.target.value)}
        />

        <TextAreaField
          label="Planned Work for Today *"
          required
          rows={2}
          placeholder="What are you planning to achieve today?"
          value={plannedWorkForToday}
          onChange={(e) => setPlannedWorkForToday(e.target.value)}
        />

        <TextAreaField
          label="Work Completed / Progress"
          rows={2}
          placeholder="What did you complete or work on?"
          value={workCompletedProgress}
          onChange={(e) => setWorkCompletedProgress(e.target.value)}
        />

        <div className="grid grid--2">
          <TextField
            label="Blocker / Dependency"
            placeholder="Any blockers or dependencies?"
            value={blockerDependency}
            onChange={(e) => setBlockerDependency(e.target.value)}
          />

          <TextField
            label="Support Required"
            placeholder="Any assistance required from peers/lead?"
            value={supportRequired}
            onChange={(e) => setSupportRequired(e.target.value)}
          />
        </div>

        <div className="grid grid--2">
          <TextField
            label="Reviewer / Lead"
            placeholder="Reviewer or Team Lead name"
            value={reviewerLead}
            onChange={(e) => setReviewerLead(e.target.value)}
          />

          <TextField
            label="Remarks"
            placeholder="Any additional notes"
            value={remarks}
            onChange={(e) => setRemarks(e.target.value)}
          />
        </div>

        <div className="modal__actions">
          <button
            type="button"
            className="btn btn--secondary"
            onClick={onClose}
            disabled={busy}
          >
            Cancel
          </button>
          <button type="submit" className="btn btn--primary" disabled={busy}>
            {busy ? 'Saving...' : statusItem ? 'Update Status' : 'Submit Daily Status'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
