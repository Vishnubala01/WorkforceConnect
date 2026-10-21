import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react';

interface FieldShellProps {
  label: string;
  hint?: string;
  error?: string;
  children: (ids: { id: string; describedBy: string | undefined; invalid: boolean }) => ReactNode;
}

function FieldShell({ label, hint, error, children }: FieldShellProps) {
  const id = useId();
  const noteId = `${id}-note`;
  const note = error ?? hint;
  return (
    <div className={error ? 'field field--invalid' : 'field'}>
      <label htmlFor={id} className="field__label">
        {label}
      </label>
      {children({ id, describedBy: note ? noteId : undefined, invalid: Boolean(error) })}
      {note && (
        <p id={noteId} className={error ? 'field__error' : 'field__hint'}>
          {note}
        </p>
      )}
    </div>
  );
}

type TextFieldProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> & {
  label: string;
  hint?: string;
  error?: string;
};

export function TextField({ label, hint, error, ...input }: TextFieldProps) {
  return (
    <FieldShell label={label} hint={hint} error={error}>
      {({ id, describedBy, invalid }) => (
        <input id={id} className="input" aria-describedby={describedBy} aria-invalid={invalid || undefined} {...input} />
      )}
    </FieldShell>
  );
}

type TextAreaFieldProps = Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'id'> & {
  label: string;
  hint?: string;
  error?: string;
};

export function TextAreaField({ label, hint, error, ...input }: TextAreaFieldProps) {
  return (
    <FieldShell label={label} hint={hint} error={error}>
      {({ id, describedBy, invalid }) => (
        <textarea id={id} className="input input--area" rows={3} aria-describedby={describedBy} aria-invalid={invalid || undefined} {...input} />
      )}
    </FieldShell>
  );
}

type SelectFieldProps = Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id'> & {
  label: string;
  hint?: string;
  error?: string;
  children: ReactNode;
};

export function SelectField({ label, hint, error, children, ...select }: SelectFieldProps) {
  return (
    <FieldShell label={label} hint={hint} error={error}>
      {({ id, describedBy, invalid }) => (
        <select id={id} className="input" aria-describedby={describedBy} aria-invalid={invalid || undefined} {...select}>
          {children}
        </select>
      )}
    </FieldShell>
  );
}
