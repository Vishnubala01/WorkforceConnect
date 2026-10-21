import { useState, type FormEvent } from 'react';
import type { Organization } from '@/core/models/types';
import { organizationService } from '@/core/services/services';
import { TextAreaField, TextField } from '@/shared/components/Fields';
import { Modal } from '@/shared/components/Modal';
import { errorMessage } from '@/shared/utils/format';

interface Props {
  open: boolean;
  /** Pass an organization to edit it; omit to create a new one. */
  organization?: Organization | null;
  onClose: () => void;
  onSaved: (organization: Organization) => void;
}

export function OrganizationFormModal({ open, organization, onClose, onSaved }: Props) {
  return (
    <Modal open={open} title={organization ? 'Edit organization' : 'New organization'} onClose={onClose}>
      <OrganizationForm organization={organization ?? null} onClose={onClose} onSaved={onSaved} />
    </Modal>
  );
}

function OrganizationForm({ organization, onClose, onSaved }: { organization: Organization | null; onClose: () => void; onSaved: (o: Organization) => void }) {
  const [name, setName] = useState(organization?.name ?? '');
  const [description, setDescription] = useState(organization?.description ?? '');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [nameError, setNameError] = useState<string | undefined>();

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    const trimmed = name.trim();
    if (trimmed.length < 2) {
      setNameError('Enter a name with at least 2 characters.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const body = { name: trimmed, description: description.trim() || null };
      const saved = organization
        ? await organizationService.update(organization.id, body)
        : await organizationService.create(body);
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
      <TextField label="Name" value={name} onChange={(e) => { setName(e.target.value); setNameError(undefined); }} error={nameError} maxLength={100} autoFocus required />
      <TextAreaField label="Description" hint="Optional. Shown on the organization card." value={description} onChange={(e) => setDescription(e.target.value)} maxLength={500} />
      <div className="form__actions">
        <button type="button" className="btn btn--secondary" onClick={onClose} disabled={busy}>
          Cancel
        </button>
        <button type="submit" className="btn btn--primary" disabled={busy}>
          {busy ? 'Saving…' : organization ? 'Save changes' : 'Create organization'}
        </button>
      </div>
    </form>
  );
}
