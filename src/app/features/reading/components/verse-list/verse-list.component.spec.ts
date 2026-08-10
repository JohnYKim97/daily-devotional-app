import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VerseListComponent } from './verse-list.component';

describe('VerseListComponent', () => {
  let component: VerseListComponent;
  let fixture: ComponentFixture<VerseListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VerseListComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(VerseListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
