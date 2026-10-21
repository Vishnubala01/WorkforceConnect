import { useState, type FormEvent } from 'react';
import type { Team } from '@/core/models/types';
import { teamService } from '@/core/services/services';
import { TextAreaField, TextField } from '@/shared/components/Fields';
import { Modal } from '@/shared/components/Modal';
import { errorMessage } from '@/shared/utils/format';

interface Props {
  open: boolean;
  organizationId: number;
  organizationName: string;
  /** Pass a team to edit it; omit to create a new one in the organization. */
  team?: Team | null;
  onClose: () => void;
  onSaved: (team: Team) => void;
}

export function TeamFormModal({ open, organizationId, organizationName, team, onClose, onSaved }: Props) {
  return (
    <Modal open={open} title={team ? 'Edit team' : `New team in ${organizationName}`} onClose={onClose}>
      <TeamForm organizationId={organizationId} team={team ?? null} onClose={onClose} onSaved={onSaved} />
    </Modal>
  );
}

function TeamForm({ organizationId, team, onClose, onSaved }: { organizationId: number; team: Team | null; onClose: () => void; onSaved: (t: Team) => void }) {
  const [name, setName] = useState(team?.name ?? '');
  const [description, setDescription] = useState(team?.description ?? '');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [nameError, setNameError] = useState<string | undefined>();

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) {
      setNameError('Enter a team name.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const desc = description.trim() || null;
      const saved = team
        ? await teamService.update(team.id, { name: trimmed, description: desc })
        : await teamService.create({ organizationId, name: trimmed, description: desc });
      onSaved(saved);
    } catch (e) {
      setError(errorMessage(e));
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} noValidate className="form">
      {error && (
        <p className="form__alert" role="alert">
          {error}
        </p>
      )}
      <TextField label="Team name" value={name} onChange={(e) => { setName(e.target.value); setNameError(undefined); }} error={nameError} maxLength={100} autoFocus required />
      <TextAreaField label="Description" hint="Optional." value={description} onChange={(e) => setDescription(e.target.value)} maxLength={500} />
      <div className="form__actions">
        <button type="button" className="btn btn--secondary" onClick={onClose} disabled={busy}>
          Cancel
        </button>
        <button type="submit" className="btn btn--primary" disabled={busy}>
          {busy ? 'Saving…' : team ? 'Save changes' : 'Create team'}
        </button>
      </div>
    </form>
  );
}
