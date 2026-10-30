import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Participant } from '@app/models/group-orders/participant.model';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-participant-card',
  imports: [CurrencyPipe],
  templateUrl: './participant-card.component.html',
  styleUrl: './participant-card.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class ParticipantCardComponent {
  readonly participant = input.required<Participant>();
  readonly isMe = input(false);
  readonly canRemove = input(false);
  readonly canEditItems = input(false);
  readonly removeClick = output<Participant>();
  readonly addDishesClick = output<void>();
  readonly itemAction = output<{
    participantId: string;
    dishId: string;
    action: 'increment' | 'decrement' | 'remove';
  }>();

  readonly title = computed(() => {
    const person = this.participant();
    if (this.isMe()) {
      return 'My Pre-Order';
    }

    return person.type === 'Owner' ? `Owner: ${person.name}` : `Guest: ${person.name}`;
  });

  onRemove(): void {
    this.removeClick.emit(this.participant());
  }

  onAddDishes(): void {
    this.addDishesClick.emit();
  }

  onItemAction(dishId: string, action: 'increment' | 'decrement' | 'remove'): void {
    this.itemAction.emit({ participantId: this.participant().id, dishId, action });
  }
}
